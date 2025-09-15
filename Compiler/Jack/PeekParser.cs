
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Jack;

public class PeekParser : Parser
{
    public record struct TokenInfo(ETokenType TokenType, string Token);

    public int Capacity => tokens.Length;

    readonly TokenInfo[] tokens;
    int tokenIndex = 0;
    int tokenCount = 0;
    TokenInfo CurrentToken => tokens[tokenIndex];

    public PeekParser(StreamReader reader, int capacity = 2) : base(reader)
    {
        tokens = new TokenInfo[capacity];
    }

    ~PeekParser()
    {
    }

    public bool Peek(int index, out TokenInfo tokenInfo)
    {
        if (index >= Capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index >= tokenCount)
        {
            base.Consume();
            for (int i = tokenCount; i <= index; i++)
            {
                if (!base.HasMoreTokens())
                {
                    tokenInfo = default;
                    return false;
                }

                base.Advandce();

                tokens[(tokenIndex + i) % Capacity] = new TokenInfo(base.TokenType(), base.Token());

                base.Consume();
            }
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
            return;
        }

        base.Advandce();
    }

    public override void Consume()
    {
        if (tokenCount > 0)
        {
            tokenIndex = (tokenIndex + 1) % Capacity;
            tokenCount--;
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