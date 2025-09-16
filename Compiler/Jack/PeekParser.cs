
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Jack;

public class PeekParser : Parser
{
    public int Capacity => tokens.Length;
    public int CacheCount { get => tokenCount; }
    public bool IsCacheConsumed { get => isCacheConsumed; }

    readonly TokenInfo[] tokens;
    int tokenIndex = 0;
    int tokenCount = 0;
    TokenInfo CurrentToken => tokens[tokenIndex];
    bool isCacheConsumed;

    public PeekParser(StreamReader reader, int capacity = 2) : base(reader)
    {
        tokens = new TokenInfo[capacity];
        isCacheConsumed = true;
    }

    ~PeekParser()
    {
    }

    public bool Peek(int index, out TokenInfo tokenInfo)
    {
        if (index == tokenCount)
        {
            tokenInfo = new TokenInfo(base.TokenType(), base.Token());
            return true;
        }

        if (index >= Capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index > tokenCount)
        {
            if (!base.HasMoreTokens())
            {
                tokenInfo = default;
                return false;
            }

            if (tokenCount == 0)
            {
                isCacheConsumed = base.IsConsumed;
            }

            var oldConsumed = base.IsConsumed;

            for (int i = tokenCount; i < index; i++)
            {
                tokenInfo = new TokenInfo(base.TokenType(), base.Token());
                tokens[(tokenIndex + i) % Capacity] = tokenInfo;

                tokenCount++;

                base.Consume();
                if (!base.HasMoreTokens())
                {
                    tokenInfo = default;
                    return false;
                }
                base.Advandce();
            }

            if (oldConsumed)
            {
                base.Consume();
            }
        }

        if (index == tokenCount)
        {
            tokenInfo = new TokenInfo(base.TokenType(), base.Token());
            return true;
        }

        tokenInfo = tokens[(tokenIndex + index) % Capacity];
        return true;
    }

    /// <summary>
    /// 是否还有更多token
    /// </summary>
    /// <returns></returns>
    public override bool HasMoreTokens()
    {
        if (tokenCount > 0)
        {
            return true;
        }

        return base.HasMoreTokens();
    }

    /// <summary>
    /// 步进读取下一条指令
    /// </summary>
    public override void Advandce()
    {
        if (tokenCount > 0)
        {
            if (!isCacheConsumed)
            {
                return;
            }

            tokenIndex = (tokenIndex + 1) % Capacity;
            tokenCount--;
            isCacheConsumed = false;
            return;
        }

        base.Advandce();
    }

    public override void Consume()
    {
        if (tokenCount > 0)
        {
            isCacheConsumed = true;
            return;
        }

        base.Consume();
    }

    public override ETokenType TokenType()
    {
        if (tokenCount > 0)
        {
            return CurrentToken.TokenType;
        }
        return base.TokenType();
    }

    public override string Token()
    {
        if (tokenCount > 0)
        {
            return CurrentToken.Token;
        }

        return base.Token();
    }
}