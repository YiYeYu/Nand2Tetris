

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using ISymbolType = Jack.IType;

namespace Jack;

public enum SymbolKind
{
    Unknown,

    Static,
    Field,
    Arg,
    Var,

    Class,
    Subroutine,
}

public record class Symbol(ISymbolType Type, string Name, SymbolKind Kind);

public record class BuildInSymbol : Symbol, IType
{
    public BuildInSymbol(ISymbolType Type, string Name, SymbolKind Kind) : base(Type, Name, Kind) { }
}

public record class VariableSymbol : Symbol
{
    public VariableSymbol(ISymbolType Type, string Name, SymbolKind Kind) : base(Type, Name, Kind) { }
}

public record class ScopedSymbol : Symbol, IScope
{
    readonly IScope scope;

    public IEnumerable<Symbol> Symbols => scope.Symbols;

    public ScopedSymbol(ISymbolType Type, string Name, SymbolKind Kind, IScope enclosingScope) : base(Type, Name, Kind)
    {
        scope = new Scope(Name, enclosingScope);
    }

    public IScope? EnclosingScope { get => scope.EnclosingScope; set => scope.EnclosingScope = value; }

    void IScope.Define(Symbol symbol)
    {
        scope.Define(symbol);
    }

    Symbol? IScope.Resolve(string name)
    {
        return scope.Resolve(name);
    }
}

public record class ClassSymbol : ScopedSymbol, IType
{
    public ClassSymbol(ISymbolType Type, string Name, SymbolKind Kind, IScope enclosingScope) : base(Type, Name, Kind, enclosingScope) { }
}

public record class SubroutineSymbol : ScopedSymbol
{
    public SubroutineSymbol(ISymbolType Type, string Name, SymbolKind Kind, IScope enclosingScope) : base(Type, Name, Kind, enclosingScope) { }
}