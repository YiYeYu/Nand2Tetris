
using System.Text;

namespace Jack;

public class Engine : TreeEngine, ICompilationEngine
{
    const string MEM_SEGMENT_ARGUMENT = "argument";
    const string MEM_SEGMENT_LOCAL = "local";
    const string MEM_SEGMENT_STATIC = "static";
    const string MEM_SEGMENT_CONSTANT = "constant";
    const string MEM_SEGMENT_THIS = "this";
    const string MEM_SEGMENT_THAT = "that";
    const string MEM_SEGMENT_POINTER = "pointer";
    const string MEM_SEGMENT_TEMP = "temp";

    const string VIRTUAL_OP_START_EXPRESSION = "startExpression";
    const string VIRTUAL_OP_START_TERM = "startTerm";

    Code code = null!;
    StringWriter buffer = new();

    #region  indent

    const string indent = "  ";

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

    List<string> expressionStack = new();

    #endregion

    public Engine()
    {
        OnStart += __onStart;

        OnAdvance += __onAdvance;
        OnConsume += __onConsume;
        OnDefine += __onDefine;
        OnEndDefine += __onEndDefine;
    }

    #region event handler

    void __onStart(object? sender, EventArgs e)
    {
        code = new Code(writer);
    }

    void __onAdvance(object? sender, EventArgs e)
    {
    }

    void __onConsume(object? sender, ConsumeEventArgs e)
    {
        if (CurrentGrammer == Grammer.Term)
        {
            if (e.Token == Const.SYMBOL_LEFT_PARENTHESES)
            {
                expressionStack.Push(e.Token);
            }
            else if (e.Token == Const.SYMBOL_RIGHT_PARENTHESES)
            {
                calExpression();
                expressionStack.Pop(); // '('
            }
        }
    }

    void __onDefine(object? sender, DefineEventArgs e)
    {
    }

    void __onEndDefine(object? sender, EndDefineEventArgs e)
    {
        // WriteComment(e.Symbol.Name);
        // WriteCommand(ECommandType.C_PUSH, "constant", symbolTable.IndexOf(e.Symbol.Name).ToString());
    }

    protected override void __onEnterGrammer(object? sender, GrammerEventArgs e)
    {
        base.__onEnterGrammer(sender, e);
        switch (e.Grammer)
        {

            case Grammer.Expression:
                expressionStack.Push(VIRTUAL_OP_START_EXPRESSION);
                break;
            case Grammer.Term:
                expressionStack.Push(VIRTUAL_OP_START_TERM);
                break;
            default:
                break;
        }
    }

    protected override void __onLeaveGrammer(object? sender, GrammerEventArgs e)
    {
        switch (e.Grammer)
        {
            // case Grammer.SubroutineBody:
            //     Depth--;
            //     break;
            case Grammer.IntegerConstant:
                WriteCommand(ECommandType.C_PUSH, "constant", parser.Token());
                break;
            case Grammer.StringConstant:
                // String.new(length)创建字符串常量
                // String.appendchar(nextchar)追加字符
                OnStringConstant();
                break;
            case Grammer.KeywordConstant:
                switch (parser.Token())
                {
                    case "true":
                        WriteCommand(ECommandType.C_PUSH, "constant", "0");
                        WriteCommand(ECommandType.C_ARITHMETIC, "not", "");
                        break;
                    case "false":
                        WriteCommand(ECommandType.C_PUSH, "constant", "0");
                        break;
                    case "null":
                        WriteCommand(ECommandType.C_PUSH, "constant", "0");
                        break;
                    case "this":
                        WriteCommand(ECommandType.C_PUSH, "pointer", "0");
                        break;
                }
                break;
            case Grammer.UnaryOp:
                switch (parser.Token())
                {
                    case "-":
                        expressionStack.Add("neg");
                        break;
                    case "~":
                        expressionStack.Add("not");
                        break;
                    default:
                        break;
                }
                break;
            case Grammer.BinaryOp:
                {
                    var op = parser.Token();
                    switch (op)
                    {
                        case "+":
                            op = "add";
                            break;
                        case "-":
                            op = "sub";
                            break;
                        case "*":
                            // op = "call Math.multiply 2";
                            break;
                        case "/":
                            // op = "call Math.divide 2";
                            break;
                        case ">":
                            op = "gt";
                            break;
                        case "<":
                            op = "lt";
                            break;
                        case "=":
                            op = "eq";
                            break;
                        case "&":
                            op = "and";
                            break;
                        case "|":
                            op = "or";
                            break;
                        default:
                            break;
                    }
                    expressionStack.Add(op);
                }
                break;
            case Grammer.Term:
                calTerm();
                break;
            case Grammer.Expression:
                calExpression();
                break;
            case Grammer.ReturnStatement:
                SubroutineSymbol subroutineSymbol = getCurrentSubroutineSymbol();
                if (subroutineSymbol.ReturnType.Name.Equals("void"))
                {
                    WriteCommand(ECommandType.C_PUSH, "constant", "0");
                }
                WriteCommand(ECommandType.C_RETURN, "", "");
                break;
            case Grammer.SubroutineCall:
                // OnLeaveSubroutineCall();
                break;
            case Grammer.DoStatement:
                WriteCommand(ECommandType.C_POP, "temp", "0");
                break;
            case Grammer.LetStatement:
                OnLeveaLetStatement();
                break;
            default:
                break;
        }

        base.__onLeaveGrammer(sender, e);
    }

    void OnStringConstant()
    {
        // String.new(length)创建字符串常量
        // String.appendchar(nextchar)追加字符
        var token = parser.Token();
        var size = token.Length;
        WriteCommand(ECommandType.C_PUSH, MEM_SEGMENT_CONSTANT, size.ToString());
        WriteCommand(ECommandType.C_CALL, "String.new", "1");
        for (int i = 0; i < size; i++)
        {
            WriteCommand(ECommandType.C_PUSH, "constant", ((int)token[i]).ToString());
            WriteCommand(ECommandType.C_CALL, "String.appendChar", "2");
        }
    }

    void OnLeaveSubroutineCall()
    {
        TreeNode node = PeekNode();

        TreeNode? classNameNode = null, varNameNode = null;

        int subroutineNameNodeIndex;

        TreeNode nameNode = node.GetChild(0);
        if (nameNode.Grammer == Grammer.SubroutineName)
        {
            subroutineNameNodeIndex = 0;
        }
        else if (nameNode.Grammer == Grammer.VarName)
        {
            varNameNode = nameNode;
            subroutineNameNodeIndex = 2;
        }
        else if (nameNode.Grammer == Grammer.ClassName)
        {
            classNameNode = nameNode;
            subroutineNameNodeIndex = 2;
        }
        else
        {
            throw CreateException($"subroutine call error, unexpected node {nameNode.Grammer}: {nameNode.Token}");
        }

        TreeNode subroutineNameNode = node.GetChild(subroutineNameNodeIndex);

        string className;
        if (classNameNode != null)
        {
            className = classNameNode.Token;
        }
        else if (varNameNode != null)
        {
            className =
            symbolTable.GetType(varNameNode.Token)?.Name!;
        }
        else
        {
            className = getCurrentClassSymbol().Name;
        }

        string fName = encodeFunctionName(className, subroutineNameNode.Token);

        TreeNode argNumNode = node.GetChild(subroutineNameNodeIndex + 2);
        int argNum = (argNumNode.Children.Count + 1) / 2;

        WriteCommand(ECommandType.C_CALL, fName, argNum.ToString());
    }

    void OnLeveaLetStatement()
    {
        TreeNode node = PeekNode();
        TreeNode varNameNode = node.GetChild(1);
    }

    #endregion

    #region EngineBase

    protected override void CompileParameterList()
    {
        SymbolKind kind = SymbolKind.Arg;

        var symbol = getCurrentSubroutineSymbol();
        var subroutineKind = symbol.Kind;

        if (subroutineKind.HasFlag(SymbolKind.Method))
        {
            var thisSymbol = new VariableSymbol(getCurrentClassSymbol(), "this", kind);
            Define(thisSymbol, kind);
            // symbol.AddArgument(thisSymbol);
            EndDefine();
        }

        if (!TryMatchGrammarType(out _))
        {
            return;
        }

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

    protected override void CompileSubroutineBody()
    {

        MatchSymbol(Const.SYMBOL_LEFT_BRACE);
        while (TryMatchGroup(ETokenType.Keyword, out _, Const.KEYWORD_VAR))
        {
            CompileGammer(Grammer.VarDec);
        }

        // 函数定义中的语句，刚定义完所有变量
        var subroutineSymbol = getCurrentSubroutineSymbol();
        var classSymbol = getCurrentClassSymbol();

        string fName = encodeFunctionName(classSymbol.Name, subroutineSymbol.Name);

        int nLocalNum = subroutineSymbol.Variables.Count;
        // if (subroutineSymbol.Kind.HasFlag(SymbolKind.Method))
        // {
        //     nLocalNum += 1;
        // }

        WriteCommand(ECommandType.C_FUNCTION, fName, nLocalNum.ToString());

        Depth++;

        if (subroutineSymbol.Kind.HasFlag(SymbolKind.Constructor))
        {
            // 利用Memory.alloc(size)分配新空间

            // pointer0 = this
            var size = classSymbol.Variables.Count(v => v.Kind.HasFlag(SymbolKind.Field));

            Console.WriteLine($"class {classSymbol.Name} size: {size}, vars: {string.Join(", ", classSymbol.Variables.Select(v => v.Name))}, kinds: {string.Join(", ", classSymbol.Variables.Select(v => v.Kind))}");

            WriteCommand(ECommandType.C_PUSH, "constant", size.ToString());
            WriteCommand(ECommandType.C_CALL, "Memory.alloc", "1");

            WriteCommand(ECommandType.C_POP, MEM_SEGMENT_POINTER, "0");
        }
        else if (subroutineSymbol.Kind.HasFlag(SymbolKind.Method))
        {
            WriteCommand(ECommandType.C_PUSH, MEM_SEGMENT_ARGUMENT, "0");
            WriteCommand(ECommandType.C_POP, MEM_SEGMENT_POINTER, "0");
        }


        CompileGammer(Grammer.Statements);
        MatchSymbol(Const.SYMBOL_RIGHT_BRACE);

        Depth--;
    }

    protected override void CompileLetStatement()
    {
        // let 表达式先求右值，再求左值
        MatchKeyword(Const.KEYWORD_LET);

        EnableBuffer();

        CompileGammer(Grammer.VarName);

        string varName = LastIdentifier;
        var varInfo = symbolTable.GetVarInfo(varName) ?? throw CreateException($"var '{varName}' not found");

        string segment = ParseSegment(varInfo);

        bool isArray = TryMatch(Const.SYMBOL_LEFT_BRACKET, ETokenType.Symbol);
        if (isArray)
        {
            PrehandleArray(segment, varInfo);
        }

        var cacheStr = DisableBuffer();

        MatchSymbol(Const.SYMBOL_EQUAL);
        CompileGammer(Grammer.Expression);
        MatchSymbol(Const.SYMBOL_SEMICOLON);

        Write(cacheStr);

        if (isArray)
        {
            WriteCommand(ECommandType.C_POP, MEM_SEGMENT_THAT, "0");
        }
        else
        {
            WriteCommand(ECommandType.C_POP, segment, varInfo.Index.ToString());
        }
    }

    protected override void CompileIfStatement()
    {
        var ifLabel = GenAutoLabel();
        var elseLabel = GenAutoLabel();
        var endLabel = GenAutoLabel();

        MatchKeyword(Const.KEYWORD_IF);
        MatchSymbol(Const.SYMBOL_LEFT_PARENTHESES);
        CompileGammer(Grammer.Expression);
        WriteCommand(ECommandType.C_IF, ifLabel, string.Empty);
        WriteCommand(ECommandType.C_GOTO, elseLabel, string.Empty);
        MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);

        MatchSymbol(Const.SYMBOL_LEFT_BRACE);
        WriteCommand(ECommandType.C_LABEL, ifLabel, string.Empty);
        CompileGammer(Grammer.Statements);
        WriteCommand(ECommandType.C_GOTO, endLabel, string.Empty);
        MatchSymbol(Const.SYMBOL_RIGHT_BRACE);

        WriteCommand(ECommandType.C_LABEL, elseLabel, string.Empty);

        if (TryMatchGroup(ETokenType.Keyword, out _, Const.KEYWORD_ELSE))
        {
            MatchKeyword(Const.KEYWORD_ELSE);
            MatchSymbol(Const.SYMBOL_LEFT_BRACE);
            CompileGammer(Grammer.Statements);
            MatchSymbol(Const.SYMBOL_RIGHT_BRACE);
        }

        WriteCommand(ECommandType.C_LABEL, endLabel, string.Empty);
    }

    protected override void CompileWhileStatement()
    {
        var startLabel = GenAutoLabel();
        var endLabel = GenAutoLabel();

        WriteCommand(ECommandType.C_LABEL, startLabel, string.Empty);

        MatchKeyword(Const.KEYWORD_WHILE);
        MatchSymbol(Const.SYMBOL_LEFT_PARENTHESES);
        CompileGammer(Grammer.Expression);
        WriteCommand(ECommandType.C_ARITHMETIC, "not", string.Empty);
        WriteCommand(ECommandType.C_IF, endLabel, string.Empty);
        MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);
        MatchSymbol(Const.SYMBOL_LEFT_BRACE);
        CompileGammer(Grammer.Statements);
        MatchSymbol(Const.SYMBOL_RIGHT_BRACE);

        WriteCommand(ECommandType.C_GOTO, startLabel, string.Empty);

        WriteCommand(ECommandType.C_LABEL, endLabel, string.Empty);
    }

    protected override void CompileTerm()
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

                string varName = LastIdentifier;
                var varInfo = symbolTable.GetVarInfo(varName) ?? throw CreateException($"var '{varName}' not found");

                string segment = ParseSegment(varInfo);

                bool isArray = TryMatch(Const.SYMBOL_LEFT_BRACKET, ETokenType.Symbol);
                if (isArray)
                {
                    PrehandleArray(segment, varInfo);
                }

                if (isArray)
                {
                    WriteCommand(ECommandType.C_PUSH, MEM_SEGMENT_THAT, "0");
                }
                else
                {
                    WriteCommand(ECommandType.C_PUSH, segment, varInfo.Index.ToString());
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

    protected override void CompileSubroutineCall()
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


        TreeNode node = PeekNode();

        TreeNode? classNameNode = null, varNameNode = null;

        int subroutineNameNodeIndex;

        TreeNode nameNode = node.GetChild(0);
        if (nameNode.Grammer == Grammer.SubroutineName)
        {
            subroutineNameNodeIndex = 0;
        }
        else if (nameNode.Grammer == Grammer.VarName)
        {
            varNameNode = nameNode;
            subroutineNameNodeIndex = 2;
        }
        else if (nameNode.Grammer == Grammer.ClassName)
        {
            classNameNode = nameNode;
            subroutineNameNodeIndex = 2;
        }
        else
        {
            throw CreateException($"subroutine call error, unexpected node {nameNode.Grammer}: {nameNode.Token}");
        }

        TreeNode subroutineNameNode = node.GetChild(subroutineNameNodeIndex);

        bool needSelf = false;

        string className;
        if (classNameNode != null)
        {
            className = classNameNode.Token;
        }
        else if (varNameNode != null)
        {
            className =
            symbolTable.GetType(varNameNode.Token)?.Name!;

            var varInfo =
            symbolTable.GetVarInfo(varNameNode.Token)!;
            var segment = ParseSegment(varInfo);
            var idx = varInfo.Index;
            WriteCommand(ECommandType.C_PUSH, segment, idx.ToString());
            needSelf = true;
        }
        else
        {
            className = getCurrentClassSymbol().Name;
            WriteCommand(ECommandType.C_PUSH, MEM_SEGMENT_POINTER, "0");
            needSelf = true;
        }

        MatchSymbol(Const.SYMBOL_LEFT_PARENTHESES);
        CompileGammer(Grammer.ExpressionList);
        MatchSymbol(Const.SYMBOL_RIGHT_PARENTHESES);

        string fName = encodeFunctionName(className, subroutineNameNode.Token);
        TreeNode argNumNode = node.GetChild(subroutineNameNodeIndex + 2);
        int argNum = (argNumNode.Children.Count + 1) / 2;
        if (needSelf)
        {
            argNum++;
        }

        WriteCommand(ECommandType.C_CALL, fName, argNum.ToString());
    }

    #endregion

    #region utils

    SubroutineSymbol getCurrentSubroutineSymbol() => CurrentSymbol as SubroutineSymbol ?? throw new ArgumentNullException(nameof(CurrentSymbol));
    ClassSymbol getCurrentClassSymbol() => symbolStack.Peek(1) as ClassSymbol ?? throw new ArgumentNullException(nameof(CurrentSymbol));

    string encodeFunctionName(string className, string name) => className + "." + name;

    void EnableBuffer()
    {
        if (code == null || code.Writer == buffer)
        {
            return;
        }

        buffer.GetStringBuilder().Clear();
        code.Writer = buffer;
    }

    string DisableBuffer(bool isAutoWrite = false)
    {
        if (code?.Writer != buffer)
        {
            return string.Empty;
        }

        string str = buffer.ToString();

        code.Writer = writer;

        if (isAutoWrite)
        {
            Write(str);
        }

        return str;
    }

    protected override void Write(string str)
    {
        if (code?.Writer == buffer)
        {
            buffer.Write(str);
            return;
        }

        base.Write(str);
    }

    void WriteIndent()
    {
        Write(currentIndent);
    }

    void WriteComment(string comment)
    {
        Write(currentIndent);
        Write($"/* {comment} */\n");
    }

    void WriteCommand(ECommandType cmd, string arg1, string arg2)
    {
        WriteInfoComment();
        WriteIndent();
        code.WriteCommand(cmd, arg1, arg2);
    }

    void WriteInfoComment()
    {
        // WriteComment($"grammar {CurrentGrammer}, symbol {CurrentSymbol}, depth {Depth}, token '{parser.Token()}', line {parser.CurrentLine}:{parser.CurrentColumn}\n");
    }

    void calTerm()
    {
        while (!expressionStack.Empty())
        {
            var op = expressionStack.Peek();
            if (op == Const.SYMBOL_LEFT_PARENTHESES || op == VIRTUAL_OP_START_EXPRESSION)
            {
                break;
            }

            expressionStack.Pop();
            if (op == Const.SYMBOL_MULTIPLY)
            {
                WriteCommand(ECommandType.C_CALL, "Math.multiply", "2");
            }
            else if (op == Const.SYMBOL_DIVIDE)
            {
                WriteCommand(ECommandType.C_CALL, "Math.divide", "2");
            }
            else if (op == VIRTUAL_OP_START_TERM)
            {
                // do nothing, just finish a term
                break;
            }
            else
            {
                WriteCommand(ECommandType.C_ARITHMETIC, op, "");
            }
        }
    }

    void calExpression()
    {
        while (!expressionStack.Empty())
        {
            var op = expressionStack.Peek();
            if (op == Const.SYMBOL_LEFT_PARENTHESES || op == Const.SYMBOL_LEFT_BRACKET)
            {
                break;
            }

            expressionStack.Pop();
            if (op == Const.SYMBOL_MULTIPLY)
            {
                WriteCommand(ECommandType.C_CALL, "Math.multiply", "2");
            }
            else if (op == Const.SYMBOL_DIVIDE)
            {
                WriteCommand(ECommandType.C_CALL, "Math.divide", "2");
            }
            else if (op == VIRTUAL_OP_START_EXPRESSION)
            {
                // do nothing, just finish a expression
                break;
            }
            else
            {
                WriteCommand(ECommandType.C_ARITHMETIC, op, "");
            }
        }
    }

    const string SYS_AUTO_LABEL_FORMAT = "$SYS_AUTO_LABEL_{0}";
    WORD autoLabelIndex = 0;

    string GenAutoLabel()
    {
        return string.Format(SYS_AUTO_LABEL_FORMAT, autoLabelIndex++);
    }

    string ParseSegment(SymbolTable.VarInfo varInfo)
    {
        string segment;
        if (varInfo.Kind.HasFlag(SymbolKind.Static))
        {
            segment = MEM_SEGMENT_STATIC;
        }
        else if (varInfo.Kind.HasFlag(SymbolKind.Arg))
        {
            segment = MEM_SEGMENT_ARGUMENT;
        }
        else if (varInfo.Kind.HasFlag(SymbolKind.Local))
        {
            segment = MEM_SEGMENT_LOCAL;
        }
        else if (varInfo.Kind.HasFlag(SymbolKind.Field))
        {
            segment = MEM_SEGMENT_THIS;
        }
        else
        {
            throw CreateException($"var '{varInfo.Symbol.Name}' invalid kind: {varInfo.Kind}");
        }

        return segment;
    }

    void PrehandleArray(string segment, SymbolTable.VarInfo varInfo)
    {
        MatchSymbol(Const.SYMBOL_LEFT_BRACKET);
        expressionStack.Push(Const.SYMBOL_LEFT_BRACKET);
        CompileGammer(Grammer.Expression);
        expressionStack.Pop();
        MatchSymbol(Const.SYMBOL_RIGHT_BRACKET);

        WriteCommand(ECommandType.C_PUSH, segment, varInfo.Index.ToString());

        WriteCommand(ECommandType.C_ARITHMETIC, "add", string.Empty);

        // that = var[index]
        WriteCommand(ECommandType.C_POP, MEM_SEGMENT_POINTER, "1");
    }

    protected override TreeNode PopNode()
    {
        TreeNode node = nodeStack.Peek();

        Console.WriteLine($"PopNode: {node.Grammer}, {node.Token}, parent {SafePeekNode()?.Grammer}, {SafePeekNode()?.Token}, expressionStack [{string.Join(", ", expressionStack)}]");

        return base.PopNode();
    }

    #endregion
}