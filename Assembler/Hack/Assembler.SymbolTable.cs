
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WORD = System.UInt16;

namespace Hack;

public partial class Assembler
{
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

}