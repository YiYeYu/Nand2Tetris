
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
    public event EventHandler<EventArgs>? OnAdvance;
    public event EventHandler<ConsumeEventArgs>? OnConsume;
    public event EventHandler<GrammerEventArgs>? OnEnterGrammer;
    public event EventHandler<GrammerEventArgs>? OnLeaveGrammer;

    protected StreamReader? reader;
    protected StreamWriter writer = null!;

    protected PeekParser parser = null!;

    protected SymbolTable symbolTable = new();

    protected string LastIdentifier { get; private set; } = string.Empty;

    IType? lastType = null;
    protected IType LastType
    {
        get => lastType ?? throw new ArgumentNullException(nameof(lastType));
    }

    public EngineBase()
    {
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

        InitSymbolTable();

        CompileGammer(Grammer.Class);

        OnEnd?.Invoke(this, EventArgs.Empty);
    }

    void InitSymbolTable()
    {
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
        CompileGammer(Grammer.ClassName);

        var symbol = new ClassSymbol(LastIdentifier);
        Define(symbol, SymbolKind.Class);
        symbolTable.PushScope(symbol);

        MatchSymbol(Const.SYMBOL_LEFT_BRACE);

        while (TryMatchGrammarClassVarDec(out _))
        {
            CompileGammer(Grammer.ClassVarDec);
        }

        while (TryMatchGrammarSubroutineDec(out _))
        {
            CompileGammer(Grammer.SubroutineDec);
        }

        MatchSymbol(Const.SYMBOL_RIGHT_BRACE);

        symbolTable.PopScope();
    }

    protected bool TryMatchGrammarClassVarDec(out Parser.TokenInfo tokenInfo)
    {
        bool isMatch = TryMatchGroup(ETokenType.Keyword, out var token, Const.KEYWORD_STATIC, Const.KEYWORD_FIELD);

        tokenInfo = new Parser.TokenInfo(ETokenType.Keyword, token ?? string.Empty);

        return isMatch;
    }

    protected virtual void CompileClassVarDec()
    {
        if (!TryMatchGrammarClassVarDec(out var tokenInfo))
        {
            return;
        }

        SymbolKind kind = tokenInfo.Token == Const.KEYWORD_STATIC ? SymbolKind.Static : SymbolKind.Field;

        Consume(); // static | field

        CompileGammer(Grammer.Type);

        CompileGammer(Grammer.VarName);

        Define(new VariableSymbol(LastType, LastIdentifier), kind);

        Advandce();
        while (parser.TokenType() == ETokenType.Symbol && parser.Token() == Const.SYMBOL_COMMA)
        {
            Consume(); // ','

            // if (TryMatchGrammarType(out _))
            // {
            //     CompileGammer(Grammer.Type);
            // }
            CompileGammer(Grammer.VarName);

            Define(new VariableSymbol(LastType, LastIdentifier), kind);

            parser.Advandce();
        }

        MatchSymbol(Const.SYMBOL_SEMICOLON);
    }

    protected bool TryMatchGrammarType(out Parser.TokenInfo tokenInfo)
    {
        if (TryMatchGroup(ETokenType.Keyword, out var token, Const.KEYWORD_INT, Const.KEYWORD_CHAR, Const.KEYWORD_BOOLEAN))
        {
            Debug.Assert(token != null);

            tokenInfo = new Parser.TokenInfo(ETokenType.Keyword, token);
            return true;
        }

        if (TryMatchIdentifier())
        {
            tokenInfo = new Parser.TokenInfo(ETokenType.Identifier, parser.Token());
            return true;
        }

        tokenInfo = default;
        return false;
    }

    protected virtual void CompileType()
    {
        if (!TryMatchGrammarType(out var tokenInfo))
        {
            throw CreateException($"CompileType failed");
        }

        if (tokenInfo.TokenType == ETokenType.Keyword)
        {
            SetLastType(symbolTable.GetType(tokenInfo.Token));
            Consume();
            return;
        }

        CompileGammer(Grammer.ClassName);

        var type = symbolTable.GetType(LastIdentifier);
        if (type == null)
        {
            // predefine type
            var symbol = new ClassSymbol(LastIdentifier);
            Define(symbol, SymbolKind.Class | SymbolKind.Other);
            type = symbol;
        }
        SetLastType(type);
    }

    void SetLastType(IType? type)
    {
        if (type == null)
        {
            throw CreateException($"SetLastType failed: {nameof(type)} is null");
        }

        lastType = type;
    }

    protected bool TryMatchGrammarSubroutineDec(out Parser.TokenInfo tokenInfo)
    {
        bool isMatch = TryMatchGroup(ETokenType.Keyword, out var token, Const.KEYWORD_CONSTRUCTOR, Const.KEYWORD_FUNCTION, Const.KEYWORD_METHOD);

        tokenInfo = new Parser.TokenInfo(ETokenType.Keyword, token ?? string.Empty);

        return isMatch;
    }

    protected virtual void CompileSubroutineDec()
    {
        if (!TryMatchGrammarSubroutineDec(out _))
        {
            throw CreateException($"CompileSubroutineDec failed");
        }
        Consume();

        symbolTable.StartSubroutine(LastIdentifier);

        if (TryMatchGroup(ETokenType.Keyword, out _, Const.KEYWORD_VOID))
        {
            Consume();
        }
        else
        {
            CompileGammer(Grammer.Type);
        }

        CompileGammer(Grammer.SubroutineName);
        MatchSymbol(Const.SYMBOL_LEFT_PARENTHESES);
        CompileGammer(Grammer.ParameterList);
        MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);
        CompileGammer(Grammer.SubroutineBody);

        symbolTable.EndSubroutine();
    }

    protected virtual void CompileParameterList()
    {
        if (!TryMatchGrammarType(out _))
        {
            return;
        }

        SymbolKind kind = SymbolKind.Arg;

        CompileGammer(Grammer.Type);
        CompileGammer(Grammer.VarName);

        Define(new VariableSymbol(LastType, LastIdentifier), kind);

        while (TryMatch(Const.SYMBOL_COMMA, ETokenType.Symbol))
        {
            Consume(); // ','

            CompileGammer(Grammer.Type);
            CompileGammer(Grammer.VarName);

            Define(new VariableSymbol(LastType, LastIdentifier), kind);
        }
    }

    protected virtual void CompileSubroutineBody()
    {
        MatchSymbol(Const.SYMBOL_LEFT_BRACE);
        while (TryMatchGroup(ETokenType.Keyword, out _, Const.KEYWORD_VAR))
        {
            CompileGammer(Grammer.VarDec);
        }
        CompileGammer(Grammer.Statements);
        MatchSymbol(Const.SYMBOL_RIGHT_BRACE);
    }

    protected virtual void CompileVarDec()
    {
        MatchKeyword(Const.KEYWORD_VAR);

        SymbolKind kind = SymbolKind.Var;

        CompileGammer(Grammer.Type);
        CompileGammer(Grammer.VarName);

        Define(new VariableSymbol(LastType, LastIdentifier), kind);

        while (TryMatch(Const.SYMBOL_COMMA, ETokenType.Symbol))
        {
            Consume(); // ','
            CompileGammer(Grammer.VarName);

            Define(new VariableSymbol(LastType, LastIdentifier), kind);
        }

        MatchSymbol(Const.SYMBOL_SEMICOLON);
    }

    protected virtual void CompileClassName()
    {
        CompileGammer(Grammer.Identifier);
    }

    protected virtual void CompileSubroutineName()
    {
        CompileGammer(Grammer.Identifier);
    }

    protected virtual void CompileVarName()
    {
        CompileGammer(Grammer.Identifier);
    }

    // 语句

    protected virtual void CompileStatements()
    {
        while (TryMatchGroup(ETokenType.Keyword, out _, Const.Statements))
        {
            CompileGammer(Grammer.Statement);
        }
    }
    protected virtual void CompileStatement()
    {
        if (!TryMatchGroup(ETokenType.Keyword, out var str, Const.Statements))
        {
            throw CreateException($"CompileStatement failed");
        }

        switch (str)
        {
            case Const.KEYWORD_LET:
                CompileGammer(Grammer.LetStatement);
                break;
            case Const.KEYWORD_IF:
                CompileGammer(Grammer.IfStatement);
                break;
            case Const.KEYWORD_WHILE:
                CompileGammer(Grammer.WhileStatement);
                break;
            case Const.KEYWORD_DO:
                CompileGammer(Grammer.DoStatement);
                break;
            case Const.KEYWORD_RETURN:
                CompileGammer(Grammer.ReturnStatement);
                break;
            default:
                throw CreateException($"CompileStatement failed, unknown statement: {str}");
        }
    }

    protected virtual void CompileLetStatement()
    {
        MatchKeyword(Const.KEYWORD_LET);
        CompileGammer(Grammer.VarName);
        if (TryMatch(Const.SYMBOL_LEFT_BRACKET, ETokenType.Symbol))
        {
            Consume();
            CompileGammer(Grammer.Expression);
            MatchSymbol(Const.SYMBOL_RIGHT_BRACKET);
        }
        MatchSymbol(Const.SYMBOL_EQUAL);
        CompileGammer(Grammer.Expression);
        MatchSymbol(Const.SYMBOL_SEMICOLON);
    }

    protected virtual void CompileIfStatement()
    {
        MatchKeyword(Const.KEYWORD_IF);
        MatchSymbol(Const.SYMBOL_LEFT_PARENTHESES);
        CompileGammer(Grammer.Expression);
        MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);
        MatchSymbol(Const.SYMBOL_LEFT_BRACE);
        CompileGammer(Grammer.Statements);
        MatchSymbol(Const.SYMBOL_RIGHT_BRACE);
        if (TryMatchGroup(ETokenType.Keyword, out _, Const.KEYWORD_ELSE))
        {
            Consume();
            MatchSymbol(Const.SYMBOL_LEFT_BRACE);
            CompileGammer(Grammer.Statements);
            MatchSymbol(Const.SYMBOL_RIGHT_BRACE);
        }
    }

    protected virtual void CompileWhileStatement()
    {
        MatchKeyword(Const.KEYWORD_WHILE);
        MatchSymbol(Const.SYMBOL_LEFT_PARENTHESES);
        CompileGammer(Grammer.Expression);
        MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);
        MatchSymbol(Const.SYMBOL_LEFT_BRACE);
        CompileGammer(Grammer.Statements);
        MatchSymbol(Const.SYMBOL_RIGHT_BRACE);
    }

    protected virtual void CompileDoStatement()
    {
        MatchKeyword(Const.KEYWORD_DO);
        CompileGammer(Grammer.SubroutineCall);
        MatchSymbol(Const.SYMBOL_SEMICOLON);
    }

    protected virtual void CompileReturnStatement()
    {
        MatchKeyword(Const.KEYWORD_RETURN);
        if (!TryMatch(Const.SYMBOL_SEMICOLON, ETokenType.Symbol))
        {
            CompileGammer(Grammer.Expression);
        }
        MatchSymbol(Const.SYMBOL_SEMICOLON);
    }

    // 表达式

    protected virtual void CompileExpression()
    {
        CompileGammer(Grammer.Term);

        while (TryMatchGroup(ETokenType.Symbol, out _, Const.BinaryOps))
        {
            Consume();
            CompileGammer(Grammer.Term);
        }
    }

    protected virtual void CompileTerm()
    {
        if (TryMatchTokenType(ETokenType.IntegerConstant))
        {
            CompileGammer(Grammer.IntegerConstant);
        }
        else if (TryMatchTokenType(ETokenType.StringConstant))
        {
            CompileGammer(Grammer.StringConstant);
        }
        else if (TryMatchGroup(ETokenType.Keyword, out _, Const.KeywordConstants))
        {
            CompileGammer(Grammer.KeywordConstant);
        }
        else if (TryMatchIdentifier())
        {
            parser.Peek(1, out var nextToken);
            if (nextToken.Token == Const.SYMBOL_DOT)
            {
                CompileGammer(Grammer.SubroutineCall);
            }
            else if (nextToken.Token == Const.SYMBOL_LEFT_PARENTHESES)
            {
                CompileGammer(Grammer.SubroutineCall);
            }
            else
            {
                CompileGammer(Grammer.VarName);
                if (TryMatch(Const.SYMBOL_LEFT_BRACKET, ETokenType.Symbol))
                {
                    Consume();
                    CompileGammer(Grammer.Expression);
                    MatchSymbol(Const.SYMBOL_RIGHT_BRACKET);
                }
            }
        }
        else if (TryMatch(Const.SYMBOL_LEFT_PARENTHESES, ETokenType.Symbol))
        {
            Consume();
            CompileGammer(Grammer.Expression);
            MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);
        }
        else if (TryMatchGroup(ETokenType.Symbol, out _, Const.UnaryOps))
        {
            Consume();
            CompileGammer(Grammer.Term);
        }
        else
        {
            throw CreateException($"CompileTerm failed");
        }
    }

    protected virtual void CompileSubroutineCall()
    {
        Advandce();
        parser.Peek(1, out var nextToken);
        if (nextToken.Token == Const.SYMBOL_DOT)
        {
            // SymbolKind kind = symbolTable.KindOf(parser.Token());
            // if (kind.HasFlag(SymbolKind.Class))
            // {
            //     CompileClassName();
            // }
            // else if (kind.HasFlag(SymbolKind.Var))
            // {
            //     CompileVarName();
            // }
            // else
            // {
            //     throw CreateException($"CompileSubroutineCall failed: {parser.Token()} is not a var name or a class name");
            // }

            CompileGammer(Grammer.Identifier);

            MatchSymbol(Const.SYMBOL_DOT);
        }

        CompileGammer(Grammer.SubroutineName);

        MatchSymbol(Const.SYMBOL_LEFT_PARENTHESES);
        CompileGammer(Grammer.ExpressionList);
        MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);
    }

    protected virtual void CompileExpressionList()
    {
        if (TryMatch(Const.SYMBOL_RIGHT_PARENTHESES, ETokenType.Symbol))
        {
            return;
        }

        CompileGammer(Grammer.Expression);
        while (TryMatch(Const.SYMBOL_COMMA, ETokenType.Symbol))
        {
            Consume();
            CompileGammer(Grammer.Expression);
        }
    }

    protected virtual void CompileBinaryOp()
    {
        if (!TryMatchGroup(ETokenType.Symbol, out _, Const.BinaryOps))
        {
            throw CreateException($"CompileBinaryOp failed");
        }

        Consume();
    }

    protected virtual void CompileUnaryOp()
    {
        if (!TryMatchGroup(ETokenType.Symbol, out _, Const.UnaryOps))
        {
            throw CreateException($"CompileUnaryOp failed");
        }

        Consume();
    }

    protected virtual void CompileKeywordConstant()
    {
        if (!TryMatchGroup(ETokenType.Keyword, out _, Const.KeywordConstants))
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

    #region helper

    protected void MatchKeyword(string str)
    {
        Match(str, ETokenType.Keyword);
    }

    protected void MatchSymbol(string c)
    {
        Match(c.ToString(), ETokenType.Symbol);
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

        LastIdentifier = parser.Token();

        Consume();

        return LastIdentifier;
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

    protected void Match(string str, ETokenType tokenType)
    {
        Debug.Assert(parser != null);

        if (!TryMatch(str, tokenType))
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

    protected bool TryMatch(string str, ETokenType tokenType)
    {
        Debug.Assert(parser != null);

        if (!TryMatchTokenType(tokenType))
        {
            return false;
        }

        return TryMatchToken(str);
    }

    protected bool TryMatchToken(string str)
    {
        return string.Equals(parser.Token(), str, StringComparison.Ordinal);
    }

    protected bool TryMatchGroup(ETokenType tokenType, out string? value, params string[] str)
    {
        Debug.Assert(parser != null);

        value = null;
        if (!TryMatchTokenType(tokenType))
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
        if (!parser.HasMoreTokens())
        {
            return;
        }

        var ok = parser.Advandce();
        if (!ok)
        {
            return;
        }

        OnAdvance?.Invoke(this, EventArgs.Empty);
    }

    protected void Consume()
    {
        parser.Consume();

        OnConsume?.Invoke(this, new ConsumeEventArgs(parser.TokenType(), parser.Token()));
    }

    protected void Define(Symbol symbol, SymbolKind kind)
    {
        symbolTable.Define(symbol, kind);
    }

    protected CompileException CreateException(string message)
    {
        return new CompileException($"{parser.CurrentLine}:{parser.CurrentColumn}: token: {parser.Token()}({parser.TokenType()}), {message}");
    }

    protected void Write(string str)
    {
        writer.Write(str);
    }

    protected void WriteLine(string str)
    {
        writer.Write(str);
        writer.Write('\n');
    }

    #endregion
}