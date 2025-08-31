
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WORD = System.UInt16;

namespace Hack;

public partial class Assembler
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
            using var reader = new StreamReader(inStream);
            var parser = new Parser(reader);

            using var outStream = File.Create(outputFileName, BUFF_SIZE);
            using var writer = new StreamWriter(outStream);
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
}