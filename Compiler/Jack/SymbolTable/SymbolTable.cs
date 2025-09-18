using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Jack;

/// <summary>
/// 符号表<br/>
/// </summary>
public class SymbolTable
{
    public record VarInfo(Symbol Symbol, SymbolKind Kind, WORD Index);

    static readonly Dictionary<string, IType> buildInTypes = new()
    {
        { "void", new BuildInSymbol("void") },
        { "int", new BuildInSymbol("int") },
        { "char", new BuildInSymbol("char") },
        { "boolean", new BuildInSymbol("boolean") },
    };

    IScope currentScope = null!;
    Scope subroutineScope;

    readonly Dictionary<Symbol, VarInfo> symbols = new();

    public SymbolTable()
    {
        currentScope = new Scope("global");
        subroutineScope = new Scope("", currentScope);
    }

    ~SymbolTable()
    {
    }

    public void PushScope(IScope scope)
    {
        if (scope == currentScope)
        {
            throw new ArgumentException("Cannot push the same scope twice");
        }

        scope.EnclosingScope = currentScope;
        currentScope = scope;
    }

    public void PopScope()
    {
        foreach (var item in currentScope.Symbols)
        {
            symbols.Remove(item);
        }

        currentScope = currentScope.EnclosingScope!;
    }

    public void StartSubroutine(string name = "")
    {
        subroutineScope.Clear();
        PushScope(subroutineScope);
    }

    public void EndSubroutine()
    {
        PopScope();
    }

    /// <summary>
    /// 定义变量对应的新标识符，并给它一个索引<br/>
    /// Static和Field标识符作用域为整个类，Arg和Var标识符作用域为当前子程序
    /// </summary>
    /// <param name="symbol"></param>
    public void Define(Symbol symbol, SymbolKind kind)
    {
        Define(new VarInfo(symbol, kind, VarCount(kind)));
    }

    void Define(VarInfo symbol)
    {
        currentScope?.Define(symbol.Symbol);
        symbols.Add(symbol.Symbol, symbol);
    }

    /// <summary>
    /// 返回kind类型的变量个数
    /// </summary>
    /// <param name="kind"></param>
    /// <returns></returns>
    public WORD VarCount(SymbolKind kind)
    {
        return (WORD)currentScope.Symbols.Count(s => KindOf(s) == kind);
    }

    public IType? GetType(string name)
    {
        buildInTypes.TryGetValue(name, out var type);
        if (type != null)
        {
            return type;
        }

        var symbol = currentScope.Resolve(name);
        return symbol?.Type ?? null;
    }

    /// <summary>
    /// 返回变量的信息
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public VarInfo? GetVarInfo(string name)
    {
        var symbol = currentScope.Resolve(name);
        if (symbol == null)
        {
            return null;
        }

        symbols.TryGetValue(symbol, out var info);
        return info;
    }

    /// <summary>
    /// 返回变量的类型
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public SymbolKind KindOf(string name)
    {
        var symbol = currentScope.Resolve(name);
        if (symbol == null)
        {
            return SymbolKind.Unknown;
        }

        return KindOf(symbol);
    }

    public SymbolKind KindOf(Symbol symbol)
    {
        symbols.TryGetValue(symbol, out var info);
        return info?.Kind ?? SymbolKind.Unknown;
    }

    /// <summary>
    /// 返回变量的类型
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public IType? TypeOf(string name)
    {
        return GetVarInfo(name)?.Symbol?.Type;
    }

    /// <summary>
    /// 返回变量的索引
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public int IndexOf(string name)
    {
        return GetVarInfo(name)?.Index ?? -1;
    }
}
