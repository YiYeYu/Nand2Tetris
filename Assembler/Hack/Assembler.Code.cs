
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WORD = System.UInt16;

namespace Hack;

public partial class Assembler
{
    public interface ICoder
    {
        void ACommand(string address);

        void CCommand(string comp, string dest, string jump);

        void LCommand(string symbol, WORD currentCommand);
    }

    public class SymbolCode : ICoder
    {
        readonly SymbolTable symbolTable;

        public SymbolCode(SymbolTable symbolTable)
        {
            this.symbolTable = symbolTable;
        }

        public void ACommand(string address)
        {
        }

        public void CCommand(string comp, string dest, string jump) { }

        public void LCommand(string symbol, WORD currentCommand)
        {
            symbolTable.AddEntry(symbol, currentCommand);
        }
    }

    public class Code : ICoder
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
        readonly SymbolTable symbolTable;
        WORD currentVariableAddress = VARIABLE_ADDRESS;

        public Code(StreamWriter writer, SymbolTable symbolTable)
        {
            this.writer = writer;
            this.symbolTable = symbolTable;
        }

        ~Code()
        {
            writer.Flush();
        }

        void Write(WORD word)
        {
            writer.WriteLine(Convert.ToString(word, 2).PadLeft(16, '0'));
        }

        public void ACommand(string symbol)
        {
            if (!symbolTable.TryGetAddress(symbol, out WORD address))
            {
                if (!WORD.TryParse(symbol, out address))
                {
                    symbolTable.AddEntry(symbol, ++currentVariableAddress);
                    address = currentVariableAddress;
                }
            }

            ACommand(address);
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

        public void LCommand(string symbol, WORD currentCommand)
        {
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

}