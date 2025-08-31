
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WORD = System.UInt16;

namespace Hack;

public partial class VMTranslator
{
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
        const WORD VARIABLE_ADDRESS = 0x000F;

        WORD buffer;

        readonly StreamWriter writer;

        public Code(StreamWriter writer)
        {
            this.writer = writer;
        }

        ~Code()
        {
            writer.Flush();
        }

        void Write(WORD word)
        {
            writer.WriteLine(Convert.ToString(word, 2).PadLeft(16, '0'));
        }

        public void WriteArithmetic(string cmd)
        {
        }

        public void WritePushPop(ECommandType cmd, string segment, WORD index)
        {
        }


    }

}