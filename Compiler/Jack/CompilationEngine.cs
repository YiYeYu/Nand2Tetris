
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Jack;

public class CompilationEngine
{
    readonly SymbolTable symbolTable;
    StreamReader? reader;
    StreamWriter? writer;

    public CompilationEngine(SymbolTable symbolTable)
    {
        this.symbolTable = symbolTable;
    }

    ~CompilationEngine()
    {
        writer?.Flush();
    }

    public void Compile(StreamReader reader, StreamWriter writer)
    {
        this.reader = reader;
        this.writer = writer;
        CompileGammer(Grammer.Class);
    }

    public void CompileGammer(Grammer grammer)
    {
    }
}