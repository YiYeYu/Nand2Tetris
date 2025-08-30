// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WORD = System.UInt16;

if (args.Length <= 0)
{
    Console.WriteLine("no asm file");
    return;
}

var assmbler = new Hack.Assembler();
assmbler.Assemble(args[0]);

namespace Hack
{
    public class Assembler
    {
        public enum ECommandType
        {
            /// <summary>
            /// A指令
            /// </summary>
            A_COMMAND,
            /// <summary>
            /// C指令
            /// </summary>
            C_COMMAND,
            /// <summary>
            /// 标签指令，伪指令
            /// </summary>
            L_COMMAND,
        }

        const int DEST_SHIFT = 3;
        public enum EDest
        {
            NULL = 0x00 << DEST_SHIFT,
            M = 0x1 << DEST_SHIFT,
            D = 0x2 << DEST_SHIFT,
            MD = 0x3 << DEST_SHIFT,
            A = 0x4 << DEST_SHIFT,
            AM = 0x5 << DEST_SHIFT,
            AD = 0x6 << DEST_SHIFT,
            AMD = 0x7 << DEST_SHIFT,
        }
        public static readonly Dictionary<string, EDest> DestMap = new(){
            {"", EDest.NULL},
            {"M", EDest.M},
            {"D", EDest.D},
            {"MD", EDest.MD},
            {"A", EDest.A},
            {"AM", EDest.AM},
            {"AD", EDest.AD},
            {"AMD", EDest.AMD},
        };

        public enum EJump
        {
            NULL = 0x0,
            JGT = 0x1,
            JEQ = 0x2,
            JGE = 0x3,
            JLT = 0x4,
            JNE = 0x5,
            JLE = 0x6,
            JMP = 0x7,
        }
        public static readonly Dictionary<string, EJump> JumpMap = new(){
            {"", EJump.NULL},
            {"JGT", EJump.JGT},
            {"JEQ", EJump.JEQ},
            {"JGE", EJump.JGE},
            {"JLT", EJump.JLT},
            {"JNE", EJump.JNE},
            {"JLE", EJump.JLE},
            {"JMP", EJump.JMP},
        };

        const int COMP_SHIFT = 6;
        public enum EComp
        {
            ZERO = 0x2A << COMP_SHIFT,
            ZERO_ = 0x6A << COMP_SHIFT,
            ONE = 0x3F << COMP_SHIFT,
            ONE_ = 0x7F << COMP_SHIFT,
            NEGATIVE_ONE = 0x3A << COMP_SHIFT,
            NEGATIVE_ONE_ = 0x7A << COMP_SHIFT,
            D = 0x0C << COMP_SHIFT,
            D_ = 0x4C << COMP_SHIFT,
            A = 0x30 << COMP_SHIFT,
            M = 0x70 << COMP_SHIFT,
            INVERSE_D = 0x0D << COMP_SHIFT,
            INVERSE_D_ = 0x4D << COMP_SHIFT,
            INVERSE_A = 0x31 << COMP_SHIFT,
            INVERSE_M = 0x71 << COMP_SHIFT,
            NEGATIVE_D = 0x0F << COMP_SHIFT,
            NEGATIVE_D_ = 0x4F << COMP_SHIFT,
            NEGATIVE_A = 0x33 << COMP_SHIFT,
            NEGATIVE_M = 0x73 << COMP_SHIFT,
            D_PLUS_ONE = 0x1F << COMP_SHIFT,
            D_PLUS_ONE_ = 0x5F << COMP_SHIFT,
            A_PLUS_ONE = 0x37 << COMP_SHIFT,
            M_PLUS_ONE = 0x77 << COMP_SHIFT,
            D_MINUS_ONE = 0x0E << COMP_SHIFT,
            D_MINUS_ONE_ = 0x4E << COMP_SHIFT,
            A_MINUS_ONE = 0x32 << COMP_SHIFT,
            M_MINUS_ONE = 0x72 << COMP_SHIFT,
            D_PLUS_A = 0x02 << COMP_SHIFT,
            D_PLUS_M = 0x42 << COMP_SHIFT,
            D_MINUS_A = 0x13 << COMP_SHIFT,
            D_MINUS_M = 0x53 << COMP_SHIFT,
            A_MINUS_D = 0x07 << COMP_SHIFT,
            M_MINUS_D = 0x47 << COMP_SHIFT,
            D_AND_A = 0x00 << COMP_SHIFT,
            D_AND_M = 0x40 << COMP_SHIFT,
            D_OR_A = 0x15 << COMP_SHIFT,
            D_OR_M = 0x55 << COMP_SHIFT,
        }
        public static readonly Dictionary<string, EComp> CompMap = new(){
            {"0", EComp.ZERO},
            {"1", EComp.ONE},
            {"-1", EComp.NEGATIVE_ONE},
            {"D", EComp.D},
            {"A", EComp.A},
            {"M", EComp.M},
            {"!D", EComp.INVERSE_D},
            {"!A", EComp.INVERSE_A},
            {"!M", EComp.INVERSE_M},
            {"-D", EComp.NEGATIVE_D},
            {"-A", EComp.NEGATIVE_A},
            {"-M", EComp.NEGATIVE_M},
            {"D+1", EComp.D_PLUS_ONE},
            {"A+1", EComp.A_PLUS_ONE},
            {"M+1", EComp.M_PLUS_ONE},
            {"D-1", EComp.D_MINUS_ONE},
            {"A-1", EComp.A_MINUS_ONE},
            {"M-1", EComp.M_MINUS_ONE},
            {"D+A", EComp.D_PLUS_A},
            {"D+M", EComp.D_PLUS_M},
            {"D-A", EComp.D_MINUS_A},
            {"D-M", EComp.D_MINUS_M},
            {"A-D", EComp.A_MINUS_D},
            {"M-D", EComp.M_MINUS_D},
            {"D&A", EComp.D_AND_A},
            {"D&M", EComp.D_AND_M},
            {"D|A", EComp.D_OR_A},
            {"D|M", EComp.D_OR_M},
        };

        public const string INPUT_SUBFIX = ".asm";
        public const string OUTPUT_SUBFIX = ".hack";
        const int BUFF_SIZE = 1024;

        public void Assemble(string path)
        {
            var fInfo = new FileInfo(path);
            var outputFileName = fInfo.FullName.Replace(INPUT_SUBFIX, OUTPUT_SUBFIX);

            try
            {
                if (File.Exists(outputFileName))
                {
                    File.Delete(outputFileName);
                }

                using var inStream = File.OpenRead(fInfo.FullName);
                var parser = new Parser(inStream);
                using var outStream = File.Create(outputFileName, BUFF_SIZE);
                var writer = new StreamWriter(outStream);
                var code = new Code(writer);

                __assemble(parser, code);

                writer.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine("Assemble failed: {0}", e);
            }
            finally
            {

            }

            Console.WriteLine("Assemble success: {0}", outputFileName);
            return;
        }

        void __assemble(Parser parser, Code code)
        {
            try
            {
                while (parser.HasMoreCommands())
                {
                    parser.Advandce();

                    var cmdType = parser.CommandType();
                    switch (cmdType)
                    {
                        case ECommandType.A_COMMAND:
                            __assembleA(parser, code);
                            break;
                        case ECommandType.C_COMMAND:
                            __assembleC(parser, code);
                            break;
                        case ECommandType.L_COMMAND:
                            __assembleL(parser, code);
                            break;
                        default:
                            throw new Exception(string.Format("unsupported command {0}", cmdType));
                    }
                }

            }
            catch (System.Exception e)
            {
                throw new Exception(
                    string.Format(
                        "line: {0}, command: {1}, type: {2}, buffer: {3}; dest: {4}, comp: {5}, jump: {6}",
                        parser.CurrentLine,
                        parser.CurrentCommand,
                        parser.CommandType(),
                        parser.Buffer,
                        parser.Dest(),
                        parser.Comp(),
                        parser.Jump()
                    ),
                    e
                );
            }
        }

        void __assembleA(Parser parser, Code code)
        {
            var symbol = parser.Symbol();
            // TODO: 符号表解析
            code.ACommand(symbol);
        }

        void __assembleC(Parser parser, Code code)
        {
            code.CCommand(parser.Comp(), parser.Dest(), parser.Jump());
        }

        void __assembleL(Parser parser, Code code)
        {
            throw new NotImplementedException();
        }

        public class Parser
        {
            const char A_COMMAND_PREFIX = '@';
            const char L_COMMAND_PREFIX = '(';
            const char L_COMMAND_SUBFIX = ')';
            static readonly Regex regASymbol = new(@"[\w\d_.$:]*");
            static readonly Regex regSymbol = new(@"[\w_.$:][\w\d_.$:]*");
            static readonly Regex regConstanst = new(@"\d+");
            static readonly Regex regCCommand = new(@"(A?M?D?)=?(.+);?(\W+)");

            readonly StreamReader reader;
            string buffer = string.Empty;
            ECommandType commandType;
            string symbol = string.Empty;
            string dest = string.Empty;
            string comp = string.Empty;
            string jump = string.Empty;
            public int CurrentLine { get; protected set; }
            public int CurrentCommand { get; protected set; }
            public string Buffer { get => buffer; }

            public Parser(Stream stream)
            {
                this.reader = new StreamReader(stream);
                CurrentLine = 0;
                CurrentCommand = 0;
            }

            ~Parser()
            {
                this.reader.Dispose();
            }

            /// <summary>
            /// 是否还有更多指令
            /// </summary>
            /// <returns></returns>
            public bool HasMoreCommands()
            {
                __eat();
                return !reader.EndOfStream;
            }

            void __eat()
            {
                if (reader.EndOfStream)
                {
                    return;
                }

                __eatWhite();
                while (__eatComment())
                {
                    __eatWhite();
                }
            }

            void __eatWhite()
            {
                for (int c = reader.Peek(); !reader.EndOfStream && char.IsWhiteSpace((char)c); c = reader.Peek())
                {
                    // do nothing
                    if (c == '\n')
                    {
                        CurrentLine++;
                    }
                    reader.Read();
                }
            }

            bool __eatComment()
            {
                // 偷懒，单斜杆注释
                if (reader.Peek() != '/')
                {
                    return false;
                }

                reader.ReadLine();
                CurrentLine++;
                return true;
            }

            /// <summary>
            /// 步进读取下一条指令
            /// 仅当HasMoreCommands为真时才能调用
            /// </summary>
            public void Advandce()
            {
                CurrentCommand++;
                CurrentLine++;

                buffer = reader.ReadLine() ?? string.Empty;
                Debug.Assert(buffer != null);

                buffer = __trim(buffer);

                char firstChar = buffer[0];

                commandType = firstChar switch
                {
                    A_COMMAND_PREFIX => ECommandType.A_COMMAND,
                    L_COMMAND_PREFIX => ECommandType.L_COMMAND,
                    _ => ECommandType.C_COMMAND,
                };

                switch (commandType)
                {
                    case ECommandType.A_COMMAND:
                        __tryParseA();
                        break;
                    case ECommandType.C_COMMAND:
                        __tryParseC();
                        break;
                    case ECommandType.L_COMMAND:
                        __tryParseL();
                        break;
                    default:
                        throw new Exception(string.Format("Advandce unsupported cmd: {0}", commandType));
                }
            }

            bool __tryParseA()
            {
                var c0 = Utils.TryGetStringCharAt(buffer, 0);
                if (c0 != A_COMMAND_PREFIX)
                {
                    return false;
                }

                symbol = buffer[1..];

                return regASymbol.IsMatch(symbol);
            }

            bool __tryParseC()
            {
                int equalIndex = buffer.IndexOf('=');
                int commonIndex = buffer.IndexOf(';');
                if (commonIndex < 0)
                {
                    commonIndex = buffer.Length;
                }

                if (commonIndex < equalIndex)
                {
                    return false;
                }

                if (equalIndex < 1)
                {
                    dest = string.Empty;
                }
                else
                {
                    dest = buffer[..equalIndex];
                }
                if (!Assembler.DestMap.ContainsKey(dest))
                {
                    return false;
                }

                if (commonIndex == buffer.Length)
                {
                    jump = string.Empty;
                }
                else
                {
                    jump = buffer[(commonIndex + 1)..];
                }
                if (!Assembler.JumpMap.ContainsKey(jump))
                {
                    return false;
                }

                comp = buffer[(equalIndex + 1)..commonIndex];
                if (!Assembler.CompMap.ContainsKey(comp))
                {
                    return false;
                }

                return true;
            }

            bool __tryParseL()
            {
                var c0 = Utils.TryGetStringCharAt(buffer, 0);
                if (c0 != L_COMMAND_PREFIX)
                {
                    return false;
                }

                var c_1 = Utils.TryGetStringCharAt(buffer, buffer.Length - 1);
                if (c_1 != L_COMMAND_SUBFIX)
                {
                    return false;
                }

                symbol = buffer[1..-1];

                return regSymbol.IsMatch(symbol);
            }

            string __trim(string str)
            {
                StringBuilder stringBuilder = new(str);

                int idx = str.IndexOf("//");
                if (idx >= 0)
                {
                    stringBuilder.Remove(idx, stringBuilder.Length - idx);
                }

                for (int i = stringBuilder.Length - 1; i >= 0; i--)
                {
                    char c = stringBuilder[i];
                    if (char.IsWhiteSpace(c))
                    {
                        stringBuilder.Remove(i, 1);
                    }
                }

                return stringBuilder.ToString();
            }

            /// <summary>
            /// 当前命令类型
            /// A指令：@value 数值 or 符号
            /// C指令：dest=comp;jump 0x111accccccdddjjj；dest为空，“=”省略；jump为空，“；”省略
            /// </summary>
            /// <returns></returns>
            public ECommandType CommandType()
            {
                return commandType;
            }

            /// <summary>
            /// 返回当前指令的符号或十进制值，当前为A指令或L指令时才能调用
            /// </summary>
            /// <returns></returns>
            public string Symbol()
            {
                return commandType switch
                {
                    ECommandType.A_COMMAND or ECommandType.L_COMMAND => symbol,
                    _ => throw new Exception(string.Format("get symbol from unsupported cmd: {0}", commandType)),
                };
            }

            /// <summary>
            /// 返回当前C指令的dest助记符（8种）
            /// </summary>
            /// <returns></returns>
            public string Dest()
            {
                return dest;
            }

            /// <summary>
            /// 返回当前C指令的cmop助记符（28种）
            /// </summary>
            /// <returns></returns>
            public string Comp()
            {
                return comp;
            }

            /// <summary>
            /// 返回当前C指令的jump助记符（8种）
            /// </summary>
            /// <returns></returns>
            public string Jump()
            {
                return jump;
            }
        }

        public class Code
        {
            public const string EMPTY = "";
            public const WORD A_COMMAND_MASK = 0x7FFF;
            public const WORD C_COMMAND_MASK = 0xFFFF;
            public const WORD C_COMMAND_HEAD_MASK = 0xE000;
            public const WORD C_COMMAND_A_MASK = 0x1000;
            public const WORD C_COMMAND_C_MASK = 0x0FC0;
            public const WORD C_COMMAND_AC_MASK = C_COMMAND_A_MASK | C_COMMAND_C_MASK;
            public const WORD C_COMMAND_D_MASK = 0x0038;
            public const WORD C_COMMAND_J_MASK = 0x0007;
            const string FORMAT = "{0:b}\n";

            WORD buffer;

            readonly StreamWriter writer;

            public Code(StreamWriter writer)
            {
                this.writer = writer;
            }

            void Write(WORD word)
            {
                writer.WriteLine(Convert.ToString(word, 2).PadLeft(16, '0'));
            }

            public void ACommand(string address)
            {
                ACommand(WORD.Parse(address));
            }

            public void ACommand(WORD address)
            {
                Write((WORD)(A_COMMAND_MASK & address));
            }

            public void CCommand(string comp, string dest = EMPTY, string jump = EMPTY)
            {
                buffer = C_COMMAND_HEAD_MASK;

                Dest(dest);
                Comp(comp);
                Jump(jump);

                Write(buffer);
            }

            public void LCommand(string symbol)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// 返回
            /// </summary>
            /// <param name="dest"></param>
            public void Dest(string dest)
            {
                if (Assembler.DestMap.TryGetValue(dest, out var eDest))
                {
                    buffer |= (WORD)((WORD)eDest & C_COMMAND_D_MASK);
                }
                else
                {
                    throw new Exception(string.Format("dest {0} not found", dest));
                }
            }

            public void Comp(string comp)
            {
                if (Assembler.CompMap.TryGetValue(comp, out var eComp))
                {
                    buffer |= (WORD)((WORD)eComp & C_COMMAND_AC_MASK);
                }
                else
                {
                    throw new Exception(string.Format("comp {0} not found", comp));
                }
            }

            public void Jump(string jump)
            {
                if (Assembler.JumpMap.TryGetValue(jump, out var eJump))
                {
                    buffer |= (WORD)((WORD)eJump & C_COMMAND_J_MASK);
                }
                else
                {
                    throw new Exception(string.Format("jump {0} not found", jump));
                }
            }
        }

        public class SymbolTable
        {
            public void AddEntry(string symbol, WORD address)
            {

            }

            public bool Contains(string symbol)
            {
                return false;
            }

            public WORD GetAddress(string symbol)
            {
                return WORD.MaxValue;
            }

            public bool TryGetAddress(string symbol, out WORD address)
            {
                address = GetAddress(symbol);
                if (!Contains(symbol))
                {
                    return false;
                }

                return true;
            }
        }

        public sealed class Utils
        {
            public static char? TryGetStringCharAt(string str, int index)
            {
                if (index >= str.Length)
                {
                    return null;
                }

                return str[index];
            }
        }

    }
}