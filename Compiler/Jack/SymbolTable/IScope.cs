

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Jack;

public interface IScope
{
    string Name { get; }
    string FullName { get { return EnclosingScope == null ? Name : $"{EnclosingScope.FullName}.{Name}"; } }
    IScope? EnclosingScope { get; set; }

    IEnumerable<Symbol> Symbols { get; }

    void Define(Symbol symbol);
    Symbol? Resolve(string name);
}