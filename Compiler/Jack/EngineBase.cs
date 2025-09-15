
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Jack;

public class EngineBase : ICompilationEngine
{
    public class CompileException : Exception
    {
        public CompileException(string message) : base(message) { }
    }

    public class ConsumeEventArgs : EventArgs
    {
        public ConsumeEventArgs(ETokenType tokenType, string token) { TokenType = tokenType; Token = token; }
        public ETokenType TokenType { get; set; }
        public string Token { get; set; }
    }
    public class GrammerEventArgs : EventArgs
    {
        public GrammerEventArgs(Grammer grammer) { Grammer = grammer; }
        public Grammer Grammer { get; set; }
    }

    public event EventHandler<EventArgs>? OnStart;
    public event EventHandler<EventArgs>? OnEnd;
    public event EventHandler<ConsumeEventArgs>? OnConsume;
    public event EventHandler<GrammerEventArgs>? OnEnterGrammer;
    public event EventHandler<GrammerEventArgs>? OnLeaveGrammer;

    protected readonly SymbolTable symbolTable;
    protected StreamReader? reader;
    protected StreamWriter? writer;

    protected PeekParser parser = null!;

    public EngineBase(SymbolTable symbolTable)
    {
        this.symbolTable = symbolTable;
    }

    ~EngineBase()
    {
        writer?.Flush();
    }

    public void Compile(StreamReader reader, StreamWriter writer)
    {
        this.reader = reader;
        this.writer = writer;

        parser = new PeekParser(reader);
        parser.Reset();

        OnStart?.Invoke(this, EventArgs.Empty);

        CompileGammer(Grammer.Class);

        OnEnd?.Invoke(this, EventArgs.Empty);
    }

    public void CompileGammer(Grammer grammer)
    {
        OnEnterGrammer?.Invoke(this, new GrammerEventArgs(grammer));

        switch (grammer)
        {
            case Grammer.Class:
                CompileClass();
                break;
            case Grammer.ClassVarDec:
                CompileClassVarDec();
                break;
            case Grammer.Type:
                CompileType();
                break;
            case Grammer.SubroutineDec:
                CompileSubroutineDec();
                break;
            case Grammer.ParameterList:
                CompileParameterList();
                break;
            case Grammer.SubroutineBody:
                CompileSubroutineBody();
                break;
            case Grammer.VarDec:
                CompileVarDec();
                break;
            case Grammer.ClassName:
                CompileClassName();
                break;
            case Grammer.SubroutineName:
                CompileSubroutineName();
                break;
            case Grammer.VarName:
                CompileVarName();
                break;
            case Grammer.Statements:
                CompileStatements();
                break;
            case Grammer.Statement:
                CompileStatement();
                break;
            case Grammer.LetStatement:
                CompileLetStatement();
                break;
            case Grammer.IfStatement:
                CompileIfStatement();
                break;
            case Grammer.WhileStatement:
                CompileWhileStatement();
                break;
            case Grammer.DoStatement:
                CompileDoStatement();
                break;
            case Grammer.ReturnStatement:
                CompileReturnStatement();
                break;
            case Grammer.Expression:
                CompileExpression();
                break;
            case Grammer.Term:
                CompileTerm();
                break;
            case Grammer.SubroutineCall:
                CompileSubroutineCall();
                break;
            case Grammer.ExpressionList:
                CompileExpressionList();
                break;
            case Grammer.BinaryOp:
                CompileBinaryOp();
                break;
            case Grammer.UnaryOp:
                CompileUnaryOp();
                break;
            case Grammer.KeywordConstant:
                CompileKeywordConstant();
                break;
            case Grammer.Identifier:
                CompileIdentifier();
                break;
            case Grammer.IntegerConstant:
                CompileIntegerConstant();
                break;
            case Grammer.StringConstant:
                CompileStringConstant();
                break;
            default:
                throw CreateException($"CompileGammer failed: {grammer}");
        }

        OnLeaveGrammer?.Invoke(this, new GrammerEventArgs(grammer));
    }

    #region compile grammer

    // 结构

    protected virtual void CompileClass()
    {
        MatchKeyword(Const.KEYWORD_CLASS);
        CompileClassName();
        MatchSymbol(Const.SYMBOL_LEFT_BRACE);
        CompileClassVarDec();
        CompileSubroutineDec();
        MatchSymbol(Const.SYMBOL_RIGHT_BRACE);
    }

    protected virtual void CompileClassVarDec()
    {
        if (!TryMatchGroup(ETokenType.Keyword, out _, false, Const.KEYWORD_STATIC, Const.KEYWORD_FIELD))
        {
            throw CreateException($"CompileClassVarDec failed");
        }
        Consume();

        CompileType();
        CompileVarName();

        Advandce();
        while (TryMatch(Const.SYMBOL_COMMA, ETokenType.Symbol, true))
        {
            Consume();

            CompileType();
            CompileVarName();
        }

        MatchSymbol(Const.SYMBOL_SEMICOLON, true);
    }

    protected virtual void CompileType()
    {
        if (TryMatchGroup(ETokenType.Keyword, out _, false, Const.KEYWORD_INT, Const.KEYWORD_CHAR, Const.KEYWORD_BOOLEAN))
        {
            Consume();
            return;
        }

        CompileClassName();
    }

    protected virtual void CompileSubroutineDec()
    {
        if (!TryMatchGroup(ETokenType.Keyword, out _, false, Const.KEYWORD_CONSTRUCTOR, Const.KEYWORD_FUNCTION, Const.KEYWORD_METHOD))
        {
            throw CreateException($"CompileSubroutineDec failed");
        }
        Consume();

        if (!TryMatchGroup(ETokenType.Keyword, out _, false, Const.KEYWORD_VOID))
        {
            CompileType();
        }

        CompileSubroutineName();
        MatchSymbol(Const.SYMBOL_LEFT_BRACE);
        CompileParameterList();
        MatchSymbol(Const.SYMBOL_RIGHT_BRACE);
        CompileSubroutineBody();
    }

    protected virtual void CompileParameterList()
    {
        bool isEmpty = false;
        try
        {
            CompileType();
        }
        catch (CompileException)
        {
            isEmpty = true;
        }

        if (isEmpty)
        {
            return;
        }

        CompileVarName();

        while (TryMatch(Const.SYMBOL_COMMA, ETokenType.Symbol))
        {
            Consume();
            CompileType();
            CompileVarName();
        }
    }

    protected virtual void CompileSubroutineBody()
    {
        MatchSymbol(Const.SYMBOL_LEFT_BRACE);
        while (TryMatchGroup(ETokenType.Keyword, out _, false, Const.KEYWORD_STATIC, Const.KEYWORD_FIELD))
        {
            CompileVarDec();
        }
        CompileStatements();
        MatchSymbol(Const.SYMBOL_RIGHT_BRACE);
    }

    protected virtual void CompileVarDec()
    {
        MatchKeyword(Const.KEYWORD_VAR);
        CompileType();
        CompileVarName();
        while (TryMatch(Const.SYMBOL_COMMA, ETokenType.Symbol))
        {
            Consume();
            CompileVarName();
        }
        MatchSymbol(Const.SYMBOL_SEMICOLON);
    }

    protected virtual void CompileClassName()
    {
        MatchIdentifier();
    }

    protected virtual void CompileSubroutineName()
    {
        MatchIdentifier();
    }

    protected virtual void CompileVarName()
    {
        MatchIdentifier();
    }

    // 语句

    protected virtual void CompileStatements()
    {
        while (TryMatchGroup(ETokenType.Keyword, out _, false, Const.KEYWORD_LET, Const.KEYWORD_IF, Const.KEYWORD_WHILE, Const.KEYWORD_DO, Const.KEYWORD_RETURN))
        {
            Consume();
            CompileStatement();
        }
    }
    protected virtual void CompileStatement()
    {
        if (!TryMatchGroup(ETokenType.Keyword, out var str, false, Const.KEYWORD_LET, Const.KEYWORD_IF, Const.KEYWORD_WHILE, Const.KEYWORD_DO, Const.KEYWORD_RETURN))
        {
            throw CreateException($"CompileStatement failed");
        }

        switch (str)
        {
            case Const.KEYWORD_LET:
                CompileLetStatement();
                break;
            case Const.KEYWORD_IF:
                CompileIfStatement();
                break;
            case Const.KEYWORD_WHILE:
                CompileWhileStatement();
                break;
            case Const.KEYWORD_DO:
                CompileDoStatement();
                break;
            case Const.KEYWORD_RETURN:
                CompileReturnStatement();
                break;
            default:
                throw CreateException($"CompileStatement failed, unknown statement: {str}");
        }
    }

    protected virtual void CompileLetStatement()
    {
        MatchKeyword(Const.KEYWORD_LET);
        CompileVarName();
        if (TryMatch(Const.SYMBOL_LEFT_BRACKET, ETokenType.Symbol))
        {
            Consume();
            CompileExpression();
            MatchSymbol(Const.SYMBOL_RIGHT_BRACKET);
        }
        MatchSymbol(Const.SYMBOL_EQUAL);
        CompileExpression();
        MatchSymbol(Const.SYMBOL_SEMICOLON);
    }

    protected virtual void CompileIfStatement()
    {
        MatchKeyword(Const.KEYWORD_IF);
        MatchSymbol(Const.SYMBOL_LEFT_PARENTHESES);
        CompileExpression();
        MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);
        MatchSymbol(Const.SYMBOL_LEFT_BRACE);
        CompileStatements();
        MatchSymbol(Const.SYMBOL_RIGHT_BRACE);
        if (TryMatchGroup(ETokenType.Keyword, out _, false, Const.KEYWORD_ELSE))
        {
            Consume();
            MatchSymbol(Const.SYMBOL_LEFT_BRACE);
            CompileStatements();
            MatchSymbol(Const.SYMBOL_RIGHT_BRACE);
        }
    }

    protected virtual void CompileWhileStatement()
    {
        MatchKeyword(Const.KEYWORD_WHILE);
        MatchSymbol(Const.SYMBOL_LEFT_PARENTHESES);
        CompileExpression();
        MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);
        MatchSymbol(Const.SYMBOL_LEFT_BRACE);
        CompileStatements();
        MatchSymbol(Const.SYMBOL_RIGHT_BRACE);
    }

    protected virtual void CompileDoStatement()
    {
        MatchKeyword(Const.KEYWORD_DO);
        CompileSubroutineCall();
        MatchSymbol(Const.SYMBOL_SEMICOLON);
    }

    protected virtual void CompileReturnStatement()
    {
        MatchKeyword(Const.KEYWORD_RETURN);
        if (!TryMatch(Const.SYMBOL_SEMICOLON, ETokenType.Symbol))
        {
            CompileExpression();
        }
        MatchSymbol(Const.SYMBOL_SEMICOLON);
    }

    // 表达式

    protected virtual void CompileExpression()
    {
        CompileTerm();

        while (TryMatchGroup(ETokenType.Symbol, out _, false, Const.BinaryOps))
        {
            Consume();
            CompileTerm();
        }
    }

    protected virtual void CompileTerm()
    {
        if (TryMatchTokenType(ETokenType.IntegerConstant))
        {
            CompileIntegerConstant();
        }
        else if (TryMatchTokenType(ETokenType.StringConstant))
        {
            CompileStringConstant();
        }
        else if (TryMatchGroup(ETokenType.Keyword, out _, false, Const.KeywordConstants))
        {
            CompileKeywordConstant();
        }
        else if (TryMatchIdentifier())
        {
            parser.Peek(1, out var nextToken);
            if (nextToken.Token == Const.SYMBOL_LEFT_PARENTHESES)
            {
                CompileSubroutineCall();
            }
            else
            {
                CompileVarName();
                if (TryMatch(Const.SYMBOL_LEFT_BRACKET, ETokenType.Symbol))
                {
                    Consume();
                    CompileExpression();
                    MatchSymbol(Const.SYMBOL_RIGHT_BRACKET);
                }
            }
        }
        else if (TryMatch(Const.SYMBOL_LEFT_PARENTHESES, ETokenType.Symbol))
        {
            Consume();
            CompileExpression();
            MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);
        }
        else if (TryMatchGroup(ETokenType.Symbol, out _, false, Const.UnaryOps))
        {
            Consume();
            CompileTerm();
        }
        else
        {
            throw CreateException($"CompileTerm failed");
        }
    }

    protected virtual void CompileSubroutineCall()
    {
        CompileIdentifier();
        MatchSymbol(Const.SYMBOL_LEFT_PARENTHESES);
        CompileExpressionList();
        MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);
    }

    protected virtual void CompileExpressionList()
    {
        if (!TryMatch(Const.SYMBOL_RIGHT_PARENTHESES, ETokenType.Symbol))
        {
            CompileExpression();
            while (TryMatch(Const.SYMBOL_COMMA, ETokenType.Symbol))
            {
                Consume();
                CompileExpression();
            }
        }
    }

    protected virtual void CompileBinaryOp()
    {
        if (!TryMatchGroup(ETokenType.Symbol, out _, false, Const.BinaryOps))
        {
            throw CreateException($"CompileBinaryOp failed");
        }

        Consume();
    }

    protected virtual void CompileUnaryOp()
    {
        if (!TryMatchGroup(ETokenType.Symbol, out _, false, Const.UnaryOps))
        {
            throw CreateException($"CompileUnaryOp failed");
        }

        Consume();
    }

    protected virtual void CompileKeywordConstant()
    {
        if (!TryMatchGroup(ETokenType.Keyword, out _, false, Const.KeywordConstants))
        {
            throw CreateException($"CompileKeywordConstant failed");
        }

        Consume();
    }

    //

    protected virtual void CompileIdentifier()
    {
        MatchIdentifier();
    }

    protected virtual void CompileIntegerConstant()
    {
        MatchIntegerConstant();
    }

    protected virtual void CompileStringConstant()
    {
        MatchStringConstant();
    }

    #endregion

    #region overwrite

    // protected virtual void OnStart() { }
    // protected virtual void OnEnd() { }
    // protected virtual void OnConsume() { }
    // protected virtual void OnEnterGrammar(Grammer grammar) { }
    // protected virtual void OnLeaveGramar(Grammer grammar) { }

    #endregion

    #region helper

    protected void MatchKeyword(string str, bool ignoreAdvance = false)
    {
        Match(str, ETokenType.Keyword, ignoreAdvance);
    }

    protected void MatchSymbol(string c, bool ignoreAdvance = false)
    {
        Match(c.ToString(), ETokenType.Symbol, ignoreAdvance);
    }

    protected bool TryMatchIdentifier()
    {
        return TryMatchTokenType(ETokenType.Identifier);
    }

    protected string MatchIdentifier()
    {
        Debug.Assert(parser != null);

        if (!TryMatchIdentifier())
        {
            throw CreateException($"MatchIdentifier failed");
        }

        Consume();

        return parser.Token();
    }

    protected bool TryMatchIntegerConstant()
    {
        return TryMatchTokenType(ETokenType.IntegerConstant);
    }

    protected string MatchStringConstant()
    {
        if (!TryMatchStringConstant())
        {
            throw CreateException($"MatchStringConstant failed");
        }

        Consume();

        return parser.Token();
    }

    protected bool TryMatchStringConstant()
    {
        return TryMatchTokenType(ETokenType.StringConstant);
    }

    protected string MatchIntegerConstant()
    {
        if (!TryMatchTokenType(ETokenType.IntegerConstant))
        {
            throw CreateException($"MatchIntegerConstant failed");
        }

        Consume();

        return parser.Token();
    }

    protected void Match(string str, ETokenType tokenType, bool ignoreAdvance = false)
    {
        Debug.Assert(parser != null);

        if (!TryMatch(str, tokenType, ignoreAdvance))
        {
            throw CreateException($"Match failed, expected: {str}({tokenType})");
        }

        Consume();
    }

    protected bool TryMatchTokenType(ETokenType tokenType, bool ignoreAdvance = false)
    {
        Debug.Assert(parser != null);

        if (!ignoreAdvance)
        {
            Advandce();
        }

        if (tokenType != parser.TokenType())
        {
            return false;
        }

        return true;
    }

    protected bool TryMatch(string str, ETokenType tokenType, bool ignoreAdvance = false)
    {
        Debug.Assert(parser != null);

        if (!TryMatchTokenType(tokenType, ignoreAdvance))
        {
            return false;
        }

        return TryMatchToken(str);
    }

    protected bool TryMatchToken(string str)
    {
        return string.Equals(parser.Token(), str, StringComparison.Ordinal);
    }

    protected bool TryMatchGroup(ETokenType tokenType, out string? value, bool ignoreAdvance = false, params string[] str)
    {
        Debug.Assert(parser != null);

        value = null;
        if (!TryMatchTokenType(tokenType, ignoreAdvance))
        {
            return false;
        }

        foreach (var s in str)
        {
            if (TryMatchToken(s))
            {
                value = s;
                return true;
            }
        }

        return false;
    }

    protected void Advandce()
    {
        parser.Advandce();
    }

    protected void Consume()
    {
        parser.Consume();

        OnConsume?.Invoke(this, new ConsumeEventArgs(parser.TokenType(), parser.Token()));
    }

    protected CompileException CreateException(string message)
    {
        return new CompileException($"{parser.CurrentLine}: token: {parser.Token()}({parser.TokenType()}), {message}");
    }

    #endregion
}