
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using IdentifierType = System.String;

namespace Jack;

/// <summary>
/// 符号表<br/>
/// </summary>
public class SymbolTable
{
    public enum VarKind
    {
        None,
        Static,
        Field,
        Arg,
        Var,
    }

    public record VarInfo(IdentifierType type, VarKind kind, WORD index);

    public SymbolTable()
    {
    }

    ~SymbolTable()
    {
    }

    public void StartSubroutine()
    {

    }

    public void Define(string name, IdentifierType type, VarKind kind)
    {
    }

    public int VarCount(VarKind kind)
    {
        return 0;
    }

    public VarInfo? GetVarInfo(string name)
    {
        return default;
    }

    public VarKind KindOf(string name)
    {
        return VarKind.None;
    }

    public IdentifierType TypeOf(string name)
    {
        return IdentifierType.Empty;
    }

    public int IndexOf(string name)
    {
        return default;
    }
}
