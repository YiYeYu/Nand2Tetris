
using System.Diagnostics;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Jack;

public class TokenEngine : ICompilationEngine
{
    protected StreamReader? reader;
    protected StreamWriter writer = null!;
    protected PeekParser parser = null!;

    public TokenEngine(SymbolTable symbolTable) { }

    public void Compile(StreamReader reader, StreamWriter writer)
    {
        this.reader = reader;
        this.writer = writer;

        parser = new PeekParser(reader);
        parser.Reset();

        WriteLine("<tokens>");
        CompileGammer(Grammer.Class);
        WriteLine("</tokens>");
    }

    public void CompileGammer(Grammer grammer)
    {
        while (parser.HasMoreTokens())
        {
            // parser.Peek(Random.Shared.Next(parser.Capacity), out _);
            parser.Advandce();
            WriteToken(parser.TokenType(), parser.Token());
            parser.Consume();
        }
    }

    protected void Write(string str)
    {
        writer.Write(str);
    }

    protected void WriteLine(string str)
    {
        writer.WriteLine(str);
    }

    protected void WriteToken(ETokenType tokenType, string str)
    {
        str = SecurityElement.Escape(str);
        string mark = tokenType.GetString();
        WriteLine($"<{mark}> {str} </{mark}>");
    }
}