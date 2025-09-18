

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using ISymbolType = Jack.IType;

namespace Jack;

[Flags]
public enum SymbolKind
{
    None = 0,
    Unknown = 1 << 1,

    // Type
    Type = Unknown << 1,
    Class = Type | Type << 1,

    // Subroutine
    Subroutine = Type << 2,
    Constructor = Subroutine | Subroutine << 1,
    Function = Subroutine | Subroutine << 2,
    Method = Subroutine | Subroutine << 3,

    // var
    Var = Subroutine << 4,
    // field
    Static = Var | Var << 1,
    Field = Var | Var << 2,
    Local = Var | Var << 3,
    Arg = Var | Var << 4,

    // other
    /// <summary>
    /// 其它文件定义的标识符
    /// </summary>
    Other = Var << 5,
}

public record class Symbol
{
    public Symbol(ISymbolType? Type, string Name)
    {
        _type = Type;
        // this.Type = Type;
        this.Name = Name;
    }


    protected ISymbolType? _type;
    public virtual ISymbolType Type { get => _type!; }
    public string Name { get; set; }

    public override string ToString()
    {
        return $"({Type.GetType().Name}:{Name})";
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}

public record class BuildInSymbol : Symbol, ISymbolType
{
    public BuildInSymbol(string Name) : base(null, Name) { _type = this; }

    public override string ToString()
    {
        return $"(buildIn:{Name})";
    }

}

public record class VariableSymbol : Symbol
{
    public VariableSymbol(ISymbolType Type, string Name) : base(Type, Name) { }
}

public record class ScopedSymbol : Symbol, IScope
{
    readonly IScope scope;

    public IEnumerable<Symbol> Symbols => scope.Symbols;

    public ScopedSymbol(ISymbolType? Type, string Name, IScope? enclosingScope) : base(Type, Name)
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

    public override string ToString()
    {
        return $"({Name})";
    }
}

public record class ClassSymbol : ScopedSymbol, ISymbolType
{
    public ClassSymbol(string Name, IScope? enclosingScope = null) : base(null, Name, enclosingScope) { _type = this; }

    public override string ToString()
    {
        return $"(class:{Name})";
    }
}

public record class SubroutineSymbol : ScopedSymbol
{
    public SubroutineSymbol(string Name, IScope? enclosingScope = null) : base(null, Name, enclosingScope) { }

    public override string ToString()
    {
        return $"(subroutine:{Name})";
    }
}