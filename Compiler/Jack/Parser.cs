
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Jack;

public class Parser
{
    const char NEW_LINE = '\n';

    readonly StreamReader reader;

    int currentLine;
    TokenType tokenType;
    StringBuilder token;


    public Parser(StreamReader reader)
    {
        this.reader = reader;

        Reset();
    }

    ~Parser()
    {
    }

    public void Reset()
    {
        reader.BaseStream.Position = 0;

        currentLine = 0;
    }

    /// <summary>
    /// 是否还有更多token
    /// </summary>
    /// <returns></returns>
    public bool HasMoreTokens()
    {
        __eat();
        return !reader.EndOfStream;
    }

    /// <summary>
    /// 步进读取下一条指令
    /// </summary>
    public void Advandce()
    {
    }

    public TokenType TokenType()
    {
        return tokenType;
    }

    public Keyword Keyword()
    {
        return default;
    }

    public SymbolType Symbol()
    {
        return default;
    }

    public WORD IntValue()
    {
        return default;
    }

    public string StringValue()
    {
        return string.Empty;
    }

    #region utils

    void __eat()
    {
        if (reader.EndOfStream)
        {
            return;
        }

        __eatWhite();
        while (__eatComment())
        {
            __eatWhite();
        }
    }

    void __eatWhite()
    {
        for (int c = reader.Peek(); !reader.EndOfStream && char.IsWhiteSpace((char)c); c = reader.Peek())
        {
            // do nothing
            if (c == NEW_LINE)
            {
                currentLine++;
            }
            reader.Read();
        }
    }

    bool __eatComment()
    {
        // 偷懒，单斜杆注释
        if (reader.Peek() != '/')
        {
            return false;
        }

        reader.ReadLine();
        currentLine++;
        return true;
    }

    #endregion
}