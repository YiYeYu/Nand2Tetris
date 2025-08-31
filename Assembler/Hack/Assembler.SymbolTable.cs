using WORD = System.UInt16;

namespace Hack;

public partial class Assembler
{
    /// <summary>
    /// 符号表，保存符号到地址的映射<br/>
    /// 有三种类型的符号<br/>
    /// 预定义符号(Predefined Symbols)<br/>
    /// 标签(Label Sysmbols)<br/>
    /// 变量(Variable Sysmbols)<br/>
    /// </summary>
    public class SymbolTable
    {
        static readonly Dictionary<string, WORD> predefined = new()
        {
            {"SP", 0x0000},
            {"LCL", 0x0001},
            {"ARG", 0x0002},
            {"THIS", 0x0003},
            {"THAT", 0x0004},
            {"R0", 0x0000},
            {"R1", 0x0001},
            {"R2", 0x0002},
            {"R3", 0x0003},
            {"R4", 0x0004},
            {"R5", 0x0005},
            {"R6", 0x0006},
            {"R7", 0x0007},
            {"R8", 0x0008},
            {"R9", 0x0009},
            {"R10", 0x000A},
            {"R11", 0x000B},
            {"R12", 0x000C},
            {"R13", 0x000D},
            {"R14", 0x000E},
            {"R15", 0x000F},
            {"SCREEN", 0x4000},
            {"KBD", 0x6000},
        };

        Dictionary<string, WORD> symbols = new(predefined) { };

        public SymbolTable()
        {
        }

        public void AddEntry(string symbol, WORD address)
        {
            if (Contains(symbol))
            {
                throw new ArgumentException($"SymbolTable dup entry {symbol}");
            }

            symbols[symbol] = address;
        }

        public bool Contains(string symbol)
        {
            return symbols.ContainsKey(symbol);
        }

        public WORD GetAddress(string symbol)
        {
            if (symbols.TryGetValue(symbol, out var address))
            {
                return address;
            }
            return WORD.MaxValue;
        }

        public bool TryGetAddress(string symbol, out WORD address)
        {
            return symbols.TryGetValue(symbol, out address);
        }
    }

}