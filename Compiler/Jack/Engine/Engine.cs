
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Jack;

public class Engine : EngineBase, ICompilationEngine
{
    public Engine(SymbolTable symbolTable) : base(symbolTable) { }
}