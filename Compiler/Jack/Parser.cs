
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Jack;

public class Parser
{
    enum CommentType
    {
        None,
        SingleLine,
        MultiLine,
        API,
    }

    const char NEW_LINE = '\n';
    const char COMMENT_START = '/';
    const char COMMENT_INNER = '*';
    const char STRING_START = '"';

    public int CurrentLine { get => currentLine; protected set => currentLine = value; }

    readonly StreamReader reader;

    int currentLine;
    ETokenType tokenType;
    readonly StringBuilder tokenBuilder = new();


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

        tokenType = default;
        tokenBuilder.Clear();
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
        var c = reader.Peek();
        if (c == STRING_START)
        {
            // TODO: 处理转义字符
            tokenType = ETokenType.StringConstant;
            reader.Read(); // "
            c = __collectOne();
            while (c != STRING_START)
            {
                c = __collectOne();
            }
            reader.Read(); // "
        }
        else if (char.IsDigit((char)c))
        {
            tokenType = ETokenType.IntegerConstant;
            c = __collectOne();
            while (!reader.EndOfStream && char.IsDigit((char)c))
            {
                c = __collectOne();
            }
            if (char.IsLetter((char)c))
            {
                throw new Exception($"line {currentLine}: invalid integer: {tokenBuilder} with trailing char: {c}");
            }
        }
        else if (SymbolTypeExtension.IsSymbol((char)c))
        {
            tokenType = ETokenType.Symbol;
            __collectOne();
        }
        else if (char.IsLetter((char)c))
        {
            while (!reader.EndOfStream && char.IsLetter((char)c))
            {
                c = __collectOne();
            }
            string token = tokenBuilder.ToString();
            if (KeywordExtension.IsKeyword(token))
            {
                tokenType = ETokenType.Keyword;
            }
            else
            {
                tokenType = ETokenType.Identifier;
            }
        }
        else
        {
            throw new Exception($"line {currentLine}: invalid token start: {c}");
        }
    }

    public ETokenType TokenType()
    {
        return tokenType;
    }

    public string Token()
    {
        return tokenBuilder.ToString();
    }

    public EKeyword Keyword()
    {
        return KeywordExtension.GetKeyword(Token());
    }

    public SymbolType Symbol()
    {
        return SymbolTypeExtension.GetSymbolType(Token()[0]);
    }

    public WORD IntValue()
    {
        return WORD.Parse(Token());
    }

    public string StringValue()
    {
        return Token();
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
        var c = reader.Peek();
        while (!reader.EndOfStream && char.IsWhiteSpace((char)c))
        {
            c = __eatOne();
        }
    }

    char __eatOne()
    {
        var c = reader.Read();
        if (c == NEW_LINE)
        {
            currentLine++;
        }

        return (char)reader.Peek();
    }

    bool __eatComment()
    {
        if (reader.Peek() != COMMENT_START)
        {
            return false;
        }

        reader.Read();

        var c = reader.Read();
        if (c == COMMENT_START)
        {
            // 单行注释
            reader.ReadLine();
            currentLine++;
        }
        else if (c == COMMENT_INNER)
        {
            // 多行注释
            while (c != COMMENT_START)
            {
                c = __eatOne();
            }
            reader.Read();
        }
        else
        {
            throw new Exception($"invalid comment {c}");
        }

        return true;
    }

    char __collectOne()
    {
        var c = reader.Read();
        tokenBuilder.Append(c);

        return (char)reader.Peek();
    }

    #endregion
}