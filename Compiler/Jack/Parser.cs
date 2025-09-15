
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

    public int CurrentLine
    {
        get => currentLine;
        protected set
        {
            if (currentLine == value)
            {
                return;
            }
            currentLine = value;
            currentColumn = 0;
        }
    }

    readonly StreamReader reader;

    int currentLine;
    int currentColumn;
    ETokenType tokenType;
    readonly StringBuilder tokenBuilder = new();
    string token = string.Empty;
    bool isConsumed;
    bool isDirty;

    int cacheIndex = 0;
    int cacheCount = 0;
    // int CacheCapacity => cache.Length;

    readonly int[] cache = new int[2];


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

        cacheCount = 0;

        CurrentLine = 1;
        currentColumn = 0;

        isConsumed = true;

        tokenType = default;
        token = string.Empty;
        tokenBuilder.Clear();
        isDirty = true;
    }

    /// <summary>
    /// 是否还有更多token
    /// </summary>
    /// <returns></returns>
    public virtual bool HasMoreTokens()
    {
        if (!isConsumed)
        {
            return true;
        }

        __eat();
        return cacheCount > 0 || !reader.EndOfStream;
    }

    /// <summary>
    /// 步进读取下一条指令
    /// </summary>
    public virtual void Advandce()
    {
        if (!isConsumed)
        {
            return;
        }

        isDirty = true;
        tokenBuilder.Clear();

        var c = __peek();
        if (c == STRING_START)
        {
            // TODO: 处理转义字符
            tokenType = ETokenType.StringConstant;
            __read(); // "
            c = __collectOne();
            while (c != STRING_START)
            {
                c = __collectOne();
            }
            __read(); // "
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
            string token = Token();
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
            throw new Exception($"line {currentLine}, column {currentColumn}: invalid token start: {c}");
        }

        isConsumed = false;
    }

    public virtual void Consume()
    {
        isConsumed = true;
    }

    public virtual ETokenType TokenType()
    {
        return tokenType;
    }

    public virtual string Token()
    {
        if (isDirty)
        {
            token = tokenBuilder.ToString();
            isDirty = false;
        }
        return token;
    }

    public EKeyword Keyword()
    {
        return KeywordExtension.GetKeyword(Token());
    }

    public SymbolType Symbol()
    {
        return SymbolTypeExtension.GetSymbolType(Token());
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
        var c = __peek();
        while (!reader.EndOfStream && char.IsWhiteSpace((char)c))
        {
            c = __eatOne();
        }
    }

    char __eatOne()
    {
        var c = __read();
        if (c == NEW_LINE)
        {
            CurrentLine++;
        }

        return (char)__peek();
    }

    bool __eatComment()
    {
        if (__peek() != COMMENT_START)
        {
            return false;
        }

        var nextChar = __peek(1);
        if (nextChar != COMMENT_INNER && nextChar != COMMENT_START)
        {
            return false;
        }

        // Console.WriteLine($"eat comment from {CurrentLine}:{currentColumn} {(char)__peek()} {(char)nextChar}");

        __read();

        var c = __read();
        if (c == COMMENT_START)
        {
            // 单行注释
            reader.ReadLine();
            CurrentLine++;
        }
        else if (c == COMMENT_INNER)
        {
            // 多行注释
            while (c != COMMENT_START)
            {
                c = __eatOne();
            }
            __read();
        }
        else
        {
            throw new Exception($"line {CurrentLine}:{currentColumn}: invalid comment ({c}:{(char)c})\n{reader.ReadToEnd()}");
        }

        return true;
    }

    char __collectOne()
    {
        var c = __read();
        tokenBuilder.Append((char)c);

        return (char)__peek();
    }

    int __read()
    {
        currentColumn++;
        if (cacheCount > 0)
        {
            cacheCount--;
            cacheIndex = (cacheIndex + 1) % cache.Length;
            return cache[cacheIndex];
        }
        return reader.Read();
    }

    int __peek(int index = 0)
    {
        if (index == 0 && cacheCount == 0)
        {
            return reader.Peek();
        }

        if (index >= cache.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        for (int i = cacheCount; i <= index; i++)
        {
            cache[cacheIndex] = reader.Read();
            cacheIndex = (cacheIndex + 1) % cache.Length;
            cacheCount++;
        }

        return cache[(cacheIndex + index) % cache.Length];
    }

    #endregion
}