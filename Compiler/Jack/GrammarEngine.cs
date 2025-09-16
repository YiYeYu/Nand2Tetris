using System.Diagnostics;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Jack;

public class GrammarEngine : EngineBase, ICompilationEngine
{
    static readonly HashSet<Grammer> ignoreGrammers = new HashSet<Grammer>()
    {
        Grammer.Type,
        Grammer.ClassName,
        Grammer.VarName,
        // Grammer.ClassVarDec,
        Grammer.SubroutineName,
        Grammer.Statement,
        Grammer.SubroutineCall,

        //
        Grammer.Identifier,
        Grammer.IntegerConstant,
        Grammer.StringConstant,
        Grammer.KeywordConstant,
    };

    const string indent = "  ";
    const bool isDebug = true;

    int depth = 0;
    int Depth
    {
        get => depth;
        set
        {
            if (value == depth)
            {
                return;
            }

            depth = value;

            currentIndent = string.Concat(Enumerable.Repeat(indent, depth));
        }
    }
    string currentIndent = string.Empty;

    public GrammarEngine(SymbolTable symbolTable) : base(symbolTable)
    {
        OnAdvance += __onAdvance;
        OnConsume += __onConsume;
        OnEnterGrammer += __onEnterGrammer;
        OnLeaveGrammer += __onLeaveGrammer;
    }

    void __onAdvance(object? sender, EventArgs e)
    {
    }

    void __onConsume(object? sender, ConsumeEventArgs e)
    {
        WriteIndent();
        WriteToken(e.TokenType, e.Token);
    }

    void __onEnterGrammer(object? sender, GrammerEventArgs e)
    {
        if (isDebug)
        {
            Console.WriteLine($"enter grammer: {e.Grammer}, depth: {Depth}, token: {parser.Token()}");
        }

        if (!ignoreGrammers.Contains(e.Grammer))
        {
            WriteIndent();
            WriteLine($"<{getMark(e.Grammer)}>");
            Depth++;
        }
    }

    void __onLeaveGrammer(object? sender, GrammerEventArgs e)
    {
        if (!ignoreGrammers.Contains(e.Grammer))
        {
            Depth--;
            WriteIndent();
            WriteLine($"</{getMark(e.Grammer)}>");
        }

        if (isDebug)
        {
            Console.WriteLine($"leave grammer: {e.Grammer}, depth: {Depth}, token: {parser.Token()}");
        }
    }

    void WriteIndent()
    {
        Write(currentIndent);
    }

    protected void WriteToken(ETokenType tokenType, string str)
    {
        str = SecurityElement.Escape(str);
        string mark = getMark(tokenType);
        WriteLine($"<{mark}> {str} </{mark}>");
    }

    string getMark(Grammer grammer) => grammer.GetXmlName();
    string getMark(ETokenType tokenType) => tokenType.GetString();
}