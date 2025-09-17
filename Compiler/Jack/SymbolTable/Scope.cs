

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Jack;

public class Scope : IScope
{
    public string Name { get; }
    public IScope? EnclosingScope { get; set; }

    readonly Dictionary<string, Symbol> symbols = new();

    public Scope(string name, IScope? enclosingScope = null) => (Name, EnclosingScope) = (name, enclosingScope);

    public void Clear() => symbols.Clear();

    public IEnumerable<Symbol> Symbols => symbols.Values;

    void IScope.Define(Symbol symbol)
    {
        if (symbols.ContainsKey(symbol.Name))
        {
            throw new ArgumentException($"Duplicate symbol {symbol.Name} in scope {((IScope)this).FullName}");
        }

        symbols.Add(symbol.Name, symbol);
    }

    Symbol? IScope.Resolve(string name)
    {
        if (!symbols.TryGetValue(name, out var symbol))
        {
            if (EnclosingScope != null)
            {
                symbol = EnclosingScope.Resolve(name);
            }
        }

        return symbol;
    }
}