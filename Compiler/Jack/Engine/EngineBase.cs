
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

    public class DefineEventArgs : EventArgs
    {
        public DefineEventArgs(Symbol symbol, SymbolKind kind) { Symbol = symbol; Kind = kind; }
        public Symbol Symbol { get; set; }
        public SymbolKind Kind { get; set; }
    }

    public class EndDefineEventArgs : EventArgs
    {
        public EndDefineEventArgs(Symbol symbol) { Symbol = symbol; }
        public Symbol Symbol { get; set; }
    }

    public event EventHandler<EventArgs>? OnStart;
    public event EventHandler<EventArgs>? OnEnd;
    public event EventHandler<EventArgs>? OnAdvance;
    public event EventHandler<ConsumeEventArgs>? OnConsume;
    public event EventHandler<DefineEventArgs>? OnDefine;
    public event EventHandler<EndDefineEventArgs>? OnEndDefine;
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

    protected readonly List<Symbol> symbolStack = new();
    protected Symbol CurrentSymbol => symbolStack.Peek();

    protected readonly List<Grammer> grammerStack = new();
    protected Grammer CurrentGrammer => grammerStack.Peek();

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
        grammerStack.Push(grammer);
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
        grammerStack.Pop();
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
        EndDefine();
    }

    protected virtual void CompileClassVarDec()
    {
        if (!TryMatchGrammarClassVarDec(out var tokenInfo))
        {
            return;
        }

        SymbolKind kind = tokenInfo.Token == Const.KEYWORD_STATIC ? SymbolKind.Static : SymbolKind.Field;

        MatchKeyword(tokenInfo.Token);

        CompileGammer(Grammer.Type);

        CompileGammer(Grammer.VarName);

        Define(new VariableSymbol(LastType, LastIdentifier, kind), kind);
        EndDefine();

        Advandce();
        while (parser.TokenType() == ETokenType.Symbol && parser.Token() == Const.SYMBOL_COMMA)
        {
            MatchSymbol(Const.SYMBOL_COMMA);

            // if (TryMatchGrammarType(out _))
            // {
            //     CompileGammer(Grammer.Type);
            // }
            CompileGammer(Grammer.VarName);

            Define(new VariableSymbol(LastType, LastIdentifier, kind), kind);
            EndDefine();

            parser.Advandce();
        }

        MatchSymbol(Const.SYMBOL_SEMICOLON);
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
            MatchKeyword(tokenInfo.Token);
            return;
        }

        CompileGammer(Grammer.ClassName);

        var type = symbolTable.GetType(LastIdentifier);
        if (type == null)
        {
            // predefine type
            var symbol = new ClassSymbol(LastIdentifier);
            Define(symbol, SymbolKind.Class | SymbolKind.Other);
            EndDefine();
            type = symbol;
        }
        SetLastType(type);
    }

    protected virtual void CompileSubroutineDec()
    {
        if (!TryMatchGrammarSubroutineDec(out var kindTokenInfo))
        {
            throw CreateException($"CompileSubroutineDec failed");
        }
        MatchKeyword(kindTokenInfo.Token); // constructor | function | method

        SymbolKind kind = kindTokenInfo.Token == Const.KEYWORD_CONSTRUCTOR ? SymbolKind.Constructor : kindTokenInfo.Token == Const.KEYWORD_FUNCTION ? SymbolKind.Function : SymbolKind.Method;

        IType returnType = null!;

        if (TryMatchGroup(ETokenType.Keyword, out var typeToken, Const.KEYWORD_VOID))
        {
            MatchKeyword(Const.KEYWORD_VOID);
            returnType = SymbolTable.BuildInSymbolVoid;
        }
        else
        {
            CompileGammer(Grammer.Type);
            returnType = LastType;
        }

        CompileGammer(Grammer.SubroutineName);

        symbolTable.StartSubroutine(LastIdentifier);

        var symbol = new SubroutineSymbol(LastIdentifier, null, kind, returnType);
        Define(symbol, kind);

        MatchSymbol(Const.SYMBOL_LEFT_PARENTHESES);
        CompileGammer(Grammer.ParameterList);
        MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);
        CompileGammer(Grammer.SubroutineBody);

        EndDefine();

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

        Define(new VariableSymbol(LastType, LastIdentifier, kind), kind);
        EndDefine();

        while (TryMatch(Const.SYMBOL_COMMA, ETokenType.Symbol))
        {
            MatchSymbol(Const.SYMBOL_COMMA);

            CompileGammer(Grammer.Type);
            CompileGammer(Grammer.VarName);

            Define(new VariableSymbol(LastType, LastIdentifier, kind), kind);
            EndDefine();
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

        SymbolKind kind = SymbolKind.Var | SymbolKind.Local;

        CompileGammer(Grammer.Type);
        CompileGammer(Grammer.VarName);

        Define(new VariableSymbol(LastType, LastIdentifier, kind), kind);
        EndDefine();

        while (TryMatch(Const.SYMBOL_COMMA, ETokenType.Symbol))
        {
            MatchSymbol(Const.SYMBOL_COMMA);
            CompileGammer(Grammer.VarName);

            Define(new VariableSymbol(LastType, LastIdentifier, kind), kind);
            EndDefine();
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
            MatchSymbol(Const.SYMBOL_LEFT_BRACKET);
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
            MatchKeyword(Const.KEYWORD_ELSE);
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
            CompileGammer(Grammer.BinaryOp);
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
                    MatchSymbol(Const.SYMBOL_LEFT_BRACKET);
                    CompileGammer(Grammer.Expression);
                    MatchSymbol(Const.SYMBOL_RIGHT_BRACKET);
                }
            }
        }
        else if (TryMatch(Const.SYMBOL_LEFT_PARENTHESES, ETokenType.Symbol))
        {
            MatchSymbol(Const.SYMBOL_LEFT_PARENTHESES);
            CompileGammer(Grammer.Expression);
            MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);
        }
        else if (TryMatchGroup(ETokenType.Symbol, out _, Const.UnaryOps))
        {
            CompileGammer(Grammer.UnaryOp);
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
            SymbolKind kind = symbolTable.KindOf(parser.Token());
            if (kind.HasFlag(SymbolKind.Var))
            {
                CompileGammer(Grammer.VarName);
            }
            else //if (kind.HasFlag(SymbolKind.Class))
            {
                CompileGammer(Grammer.ClassName);
            }

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
            MatchSymbol(Const.SYMBOL_COMMA);
            CompileGammer(Grammer.Expression);
        }
    }

    protected virtual void CompileBinaryOp()
    {
        if (!TryMatchGroup(ETokenType.Symbol, out var token, Const.BinaryOps) || token == null)
        {
            throw CreateException($"CompileBinaryOp failed");
        }

        MatchSymbol(token);
    }

    protected virtual void CompileUnaryOp()
    {
        if (!TryMatchGroup(ETokenType.Symbol, out var token, Const.UnaryOps) || token == null)
        {
            throw CreateException($"CompileUnaryOp failed");
        }

        MatchSymbol(token);
    }

    protected virtual void CompileKeywordConstant()
    {
        if (!TryMatchGroup(ETokenType.Keyword, out var token, Const.KeywordConstants) || token == null)
        {
            throw CreateException($"CompileKeywordConstant failed");
        }

        MatchKeyword(token);
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

    // 终结符

    protected virtual void MatchKeyword(string str)
    {
        Match(str, ETokenType.Keyword);
    }

    protected virtual void MatchSymbol(string c)
    {
        Match(c.ToString(), ETokenType.Symbol);
    }

    protected virtual void MatchIdentifier()
    {
        Debug.Assert(parser != null);

        if (!TryMatchIdentifier())
        {
            throw CreateException($"MatchIdentifier failed");
        }

        LastIdentifier = parser.Token();

        Consume();
    }

    protected virtual void MatchStringConstant()
    {
        if (!TryMatchStringConstant())
        {
            throw CreateException($"MatchStringConstant failed");
        }

        Consume();
    }

    protected virtual void MatchIntegerConstant()
    {
        if (!TryMatchTokenType(ETokenType.IntegerConstant))
        {
            throw CreateException($"MatchIntegerConstant failed");
        }

        Consume();
    }

    #endregion

    #region helper

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

        OnDefined(symbol, kind);

        OnDefine?.Invoke(this, new DefineEventArgs(symbol, kind));
    }

    void OnDefined(Symbol symbol, SymbolKind kind)
    {
        switch (CurrentGrammer)
        {
            case Grammer.ClassVarDec:
                ClassSymbol? classSymbol = CurrentSymbol as ClassSymbol ?? throw new ArgumentNullException(nameof(CurrentSymbol));
                classSymbol.AddVariable(symbol as VariableSymbol ?? throw new ArgumentNullException(nameof(symbol)));
                break;
            case Grammer.SubroutineDec:
                classSymbol = CurrentSymbol as ClassSymbol ?? throw new ArgumentNullException(nameof(CurrentSymbol));
                classSymbol.AddSubroutine(symbol as SubroutineSymbol ?? throw new ArgumentNullException(nameof(symbol)));
                break;
            case Grammer.ParameterList:
                var subroutineSymbol = CurrentSymbol as SubroutineSymbol ?? throw new ArgumentNullException(nameof(CurrentSymbol));
                subroutineSymbol.AddArgument(symbol as VariableSymbol ?? throw new ArgumentNullException(nameof(symbol)));
                break;
            case Grammer.VarDec:
                subroutineSymbol = CurrentSymbol as SubroutineSymbol ?? throw new ArgumentNullException(nameof(CurrentSymbol));
                subroutineSymbol.AddVariable(symbol as VariableSymbol ?? throw new ArgumentNullException(nameof(symbol)));
                break;
            default:
                break;
        }
        symbolStack.Push(symbol);
    }

    protected void EndDefine()
    {
        OnEndDefine?.Invoke(this, new EndDefineEventArgs(CurrentSymbol));

        symbolStack.Pop();
    }

    protected CompileException CreateException(string message)
    {
        return new CompileException($"{parser.CurrentLine}:{parser.CurrentColumn}: token: {parser.Token()}({parser.TokenType()}), {message}");
    }

    protected virtual void Write(string str)
    {
        writer.Write(str);
    }

    protected void WriteLine(string str)
    {
        Write(str);
        Write("\n");
    }

    protected bool TryMatchGrammarClassVarDec(out Parser.TokenInfo tokenInfo)
    {
        bool isMatch = TryMatchGroup(ETokenType.Keyword, out var token, Const.KEYWORD_STATIC, Const.KEYWORD_FIELD);

        tokenInfo = new Parser.TokenInfo(ETokenType.Keyword, token ?? string.Empty);

        return isMatch;
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

    protected bool TryMatchIntegerConstant()
    {
        return TryMatchTokenType(ETokenType.IntegerConstant);
    }

    protected bool TryMatchStringConstant()
    {
        return TryMatchTokenType(ETokenType.StringConstant);
    }

    protected bool TryMatchIdentifier()
    {
        return TryMatchTokenType(ETokenType.Identifier);
    }

    #endregion
}