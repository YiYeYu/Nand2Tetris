using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Jack;

/// <summary>
/// 符号表<br/>
/// </summary>
public class SymbolTable
{
    public record VarInfo(Symbol Symbol, WORD Index);

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

    void PushScope(IScope scope)
    {
        scope.EnclosingScope = currentScope;
        currentScope = scope;
    }

    void PopScope()
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
    public void Define(Symbol symbol)
    {
        Define(new VarInfo(symbol, VarCount(symbol.Kind)));
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
        return (WORD)currentScope.Symbols.Count(s => s.Kind == kind);
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
        return GetVarInfo(name)?.Symbol.Kind ?? SymbolKind.Unknown;
    }

    /// <summary>
    /// 返回变量的类型
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public IType? TypeOf(string name)
    {
        return GetVarInfo(name)?.Symbol.Type;
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
