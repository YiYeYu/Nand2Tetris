
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using WORD = System.UInt16;

namespace Hack;

public partial class VMTranslator
{
    /// <summary>
    /// 0-15虚拟寄存器<br/>
    /// R0 SP,
    /// R1 LCL,
    /// R2 AGR,
    /// R3 THIS,
    /// R4 THAT,
    /// R5-R12 temp
    /// R13-R15 通用
    /// <br/>
    /// 16-255静态变量<br/>
    /// 256-2047栈
    /// 2048-16383堆
    /// 16384-24575IO内存映像
    /// </summary>
    public class Code
    {
        const WORD WORD_ONE = 0x0001;
        const WORD WORD_MIN = 0x8000;
        const WORD WORD_NEG_ONE = 0xFFFF;

        record WordInterval(WORD Start, WORD End);

        static readonly WordInterval POINTER_INTERVAL = new(0x0003, 0x0004);
        static readonly WordInterval TEMP_INTERVAL = new(0x0005, 0x000C);
        static readonly WordInterval COMMON_INTERVAL = new(0x000D, 0x000F);
        static readonly WordInterval STATIC_INTERVAL = new(0x0010, 0x00FF);
        static readonly WordInterval STACK_INTERVAL = new(0x0100, 0x0AFF);
        static readonly WordInterval HEAP_INTERVAL = new(0x0B00, 0x3FFF);
        static readonly WordInterval IOMAP_INTERVAL = new(0x4000, 0x6000);

        static readonly string[] TEMP_REGISTERS = new string[] { "R13", "R14", "R15" };
        static readonly string TEMP_REGISTER = TEMP_REGISTERS[0];

        // x | -x = -1, 0|-0=0
        const string CMD_D_EQ_ZERO = "D=D-A\nA=-D\nD=D|A\nD=!D\n";
        static readonly string CMD_D_LT_ZERO = $"A={WORD_MIN}\nD=D&A\n";

        const string MEM_SEGMENT_ARGRUMENT = "argument";
        const string MEM_SEGMENT_LOCAL = "local";
        const string MEM_SEGMENT_STATIC = "static";
        const string MEM_SEGMENT_CONSTANT = "constant";
        const string MEM_SEGMENT_THIS = "this";
        const string MEM_SEGMENT_THAT = "that";
        const string MEM_SEGMENT_POINTER = "pointer";
        const string MEM_SEGMENT_TEMP = "temp";
        static readonly Dictionary<string, string> MemSegmentAddr = new(){
            {MEM_SEGMENT_LOCAL, "LCL"},
            {MEM_SEGMENT_ARGRUMENT, "ARG"},
            {MEM_SEGMENT_THIS, "THIS"},
            {MEM_SEGMENT_THAT, "THAT"},
            {MEM_SEGMENT_STATIC, STATIC_INTERVAL.Start.ToString()},
            {MEM_SEGMENT_POINTER, POINTER_INTERVAL.Start.ToString()},
            {MEM_SEGMENT_TEMP, TEMP_INTERVAL.Start.ToString()},
        };

        const string ARITHMETIC_ADD = "add";
        const string ARITHMETIC_SUB = "sub";
        const string ARITHMETIC_NEG = "neg";
        const string ARITHMETIC_EQ = "eq";
        const string ARITHMETIC_GT = "gt";
        const string ARITHMETIC_LT = "lt";
        const string ARITHMETIC_AND = "and";
        const string ARITHMETIC_OR = "or";
        const string ARITHMETIC_NOT = "not";

        delegate void DWriteCommand(Code code, ECommandType cmd, string arg1, string arg2);
        static readonly Dictionary<ECommandType, DWriteCommand> WriteCommands = new(){
            {ECommandType.C_ARITHMETIC, WriteArithmetic},
            {ECommandType.C_PUSH, WritePush},
            {ECommandType.C_POP, WritePop},
        };

        static readonly Dictionary<string, string> ArithmeticCommands = new(){
            {ARITHMETIC_NEG, "-D"},
            {ARITHMETIC_NOT, "!D"},
            {ARITHMETIC_ADD, "D+A"},
            {ARITHMETIC_SUB, "D-A"},
            {ARITHMETIC_AND, "D+A"},
            {ARITHMETIC_OR, "D+A"},

            {ARITHMETIC_EQ, "D+A"},
            {ARITHMETIC_GT, "D+A"},
            {ARITHMETIC_LT, "D+A"},
        };

        readonly StreamWriter writer;
        readonly SymbolTable symbolTable = new();
        string contextFile = string.Empty;
        WORD existFileMaxIndex = 0;
        WORD currentFileMaxIndex = 0;
        WORD spPtr;
        WORD heapPtr;
        WORD staticPtr;
        WORD[] buffers = new WORD[2];

        public Code(StreamWriter writer)
        {
            this.writer = writer;

            spPtr = STACK_INTERVAL.Start;
            heapPtr = HEAP_INTERVAL.Start;
            staticPtr = STATIC_INTERVAL.Start;
        }

        ~Code()
        {
            writer.Flush();
        }

        void Write(string cmd)
        {
            writer.Write(cmd);
        }

        public void SetFile(string fileName)
        {
            contextFile = fileName;
            currentFileMaxIndex = 0;
        }

        public void CloseFile()
        {
            writer.Flush();
            contextFile = string.Empty;
            existFileMaxIndex += currentFileMaxIndex;
            currentFileMaxIndex = 0;
        }

        public void WriteCommand(ECommandType cmd, string arg1, string arg2)
        {
            // Console.WriteLine($"WriteCommand: {cmd}: {arg1}, {arg2}");
            WriteCommands[cmd](this, cmd, arg1, arg2);
        }

        public static void WriteArithmetic(Code code, ECommandType _, string cmd, string __)
        {
            switch (cmd)
            {
                case ARITHMETIC_NEG:
                case ARITHMETIC_NOT:
                    code.__PopD();
                    break;
                case ARITHMETIC_ADD:
                case ARITHMETIC_SUB:
                case ARITHMETIC_EQ:
                case ARITHMETIC_GT:
                case ARITHMETIC_LT:
                case ARITHMETIC_AND:
                case ARITHMETIC_OR:
                    code.__PopD();
                    code.__PopA();
                    break;
                default:
                    throw new ArgumentException($"unsupported cmd {cmd}");
            }

            switch (cmd)
            {
                case ARITHMETIC_NEG:
                    code.Write($"D=-D\n");
                    break;
                case ARITHMETIC_NOT:
                    code.Write($"D=!D\n");
                    break;
                case ARITHMETIC_ADD:
                    code.Write($"D=D+A\n");
                    break;
                case ARITHMETIC_SUB:
                    code.Write($"D=D-A\n");
                    break;
                case ARITHMETIC_AND:
                    code.Write($"D=D&A\n");
                    break;
                case ARITHMETIC_OR:
                    code.Write($"D=D|A\n");
                    break;
                case ARITHMETIC_GT:
                    code.Write($"D=A-D\n");
                    code.Write(CMD_D_LT_ZERO);
                    break;
                case ARITHMETIC_LT:
                    code.Write($"D=D-A\n");
                    code.Write(CMD_D_LT_ZERO);
                    break;
                case ARITHMETIC_EQ:
                    code.Write(CMD_D_EQ_ZERO);
                    break;
                default:
                    throw new ArgumentException($"unsupported cmd {cmd}");
            }
            code.__PushD();
        }

        /// <summary>
        /// TODO: check segment index
        /// </summary>
        /// <param name="code"></param>
        /// <param name="_"></param>
        /// <param name="segment"></param>
        /// <param name="index"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void WritePush(Code code, ECommandType _, string segment, string index)
        {
            if (string.IsNullOrEmpty(index))
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (segment == MEM_SEGMENT_CONSTANT)
            {
                code.__DCopy(index);
                code.__PushD();
                return;
            }

            __WritePushPopAddress(code, segment, index);

            code.Write($"@D=M\n");
            code.__PushD();
        }

        public static void WritePop(Code code, ECommandType _, string segment, string index)
        {
            if (string.IsNullOrEmpty(index))
            {
                throw new ArgumentNullException(nameof(index));
            }

            __WritePushPopAddress(code, segment, index);

            code.__A2Temp();
            code.__PopD();
            code.__Temp2A();
            code.Write($"@M=D\n");
        }


        static void __WritePushPopAddress(Code code, string segment, string index)
        {
            string address;
            switch (segment)
            {
                case MEM_SEGMENT_LOCAL:
                case MEM_SEGMENT_ARGRUMENT:
                case MEM_SEGMENT_THIS:
                case MEM_SEGMENT_THAT:
                    address = MemSegmentAddr[segment];
                    code.__DCopy(index);
                    code.Write($"@{address}\nA=M\nAD=D+A\n");
                    break;
                case MEM_SEGMENT_POINTER:
                case MEM_SEGMENT_TEMP:
                    address = MemSegmentAddr[segment];
                    code.__DCopy(index);
                    code.Write($"@{address}\nA=D+A\n");
                    break;
                case MEM_SEGMENT_STATIC:
                    WORD curIdex = WORD.Parse(index);
                    if (curIdex > code.currentFileMaxIndex)
                    {
                        code.currentFileMaxIndex = curIdex;
                    }
                    address = MemSegmentAddr[segment];
                    code.__DCopy(index);
                    code.Write($"@{code.existFileMaxIndex}\nD=D+A\n");
                    code.Write($"@{address}\nA=D+A\n");
                    break;
                default:
                    throw new ArgumentException($"unsupported segment {segment}");
            }
        }

        /// <summary>
        /// D=value
        /// A=?
        /// </summary>
        /// <param name="value"></param>
        void __DCopy(string value)
        {
            //D = value
            Write($"@{value}\nD=A\n");
        }

        /// <summary>
        /// M[SP++]=D
        /// </summary>
        void __PushD()
        {
            //M[SP] = D
            //SP++
            Write($"@SP\nA=M\nM=D\n@SP\nM=M+1\n");
        }

        /// <summary>
        /// M[SP++]=value
        /// D=value
        /// A=SP-1
        /// </summary>
        /// <param name="value"></param>
        void __Push(string value)
        {
            //D = value
            //M[SP] = D
            //SP++
            __DCopy(value);
            __PushD();
        }

        /// <summary>
        /// D = M[SP--]
        /// A = SP
        /// </summary>
        void __PopD()
        {
            //SP--
            //D = M[SP]
            Write($"@SP\nAM=M-1\nD=M\n");
        }

        /// <summary>
        /// A = M[SP--]
        /// </summary>
        void __PopA()
        {
            //SP--
            //A = M[SP]
            Write($"@SP\nAM=M-1\nA=M\n");
        }

        void __D2Temp(WORD index = 0)
        {
            Write($"@{TEMP_REGISTER[index]}\nM=D\n");
        }

        void __Temp2D(WORD index = 0)
        {
            Write($"@{TEMP_REGISTER[index]}\nD=M\n");
        }

        void __A2Temp(WORD index = 0)
        {
            Write($"@{TEMP_REGISTER[index]}\nM=A\n");
        }

        void __Temp2A(WORD index = 0)
        {
            Write($"@{TEMP_REGISTER[index]}\nA=M\n");
        }
    }

}