
using System.Text;

namespace Jack;

public class Engine : TreeEngine, ICompilationEngine
{
    const string MEM_SEGMENT_ARGRUMENT = "argument";
    const string MEM_SEGMENT_LOCAL = "local";
    const string MEM_SEGMENT_STATIC = "static";
    const string MEM_SEGMENT_CONSTANT = "constant";
    const string MEM_SEGMENT_THIS = "this";
    const string MEM_SEGMENT_THAT = "that";
    const string MEM_SEGMENT_POINTER = "pointer";
    const string MEM_SEGMENT_TEMP = "temp";

    Code code = null!;
    StringBuilder stringBuilder = new();
    StringBuilder? buffer = null;

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

        if (e.Grammer == Grammer.Statements && grammerStack.Peek(1) == Grammer.SubroutineBody)
        {
            // 函数定义中的语句，刚定义完所有变量
            var subroutineSymbol = getCurrentSubroutineSymbol();
            var classSymbol = getCurrentClassSymbol();

            string fName = encodeFunctionName(classSymbol.Name, subroutineSymbol.Name);

            int nLocalNum = subroutineSymbol.Variables.Count;
            if (subroutineSymbol.Kind.HasFlag(SymbolKind.Method))
            {
                nLocalNum += 1;
            }

            WriteCommand(ECommandType.C_FUNCTION, fName, nLocalNum.ToString());

            Depth++;
        }
    }

    protected override void __onLeaveGrammer(object? sender, GrammerEventArgs e)
    {
        switch (e.Grammer)
        {
            case Grammer.SubroutineBody:
                Depth--;
                break;
            case Grammer.IntegerConstant:
                WriteCommand(ECommandType.C_PUSH, "constant", parser.Token());
                break;
            case Grammer.StringConstant:
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
                OnLeaveSubroutineCall();
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
        TreeNode argNumNode = node.GetChild(subroutineNameNodeIndex + 2);

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

    protected override void CompileLetStatement()
    {
        MatchKeyword(Const.KEYWORD_LET);

        CompileGammer(Grammer.VarName);

        string varName = LastIdentifier;
        var varInfo = symbolTable.GetVarInfo(varName) ?? throw CreateException($"var '{varName}' not found");

        string segment = ParseSegment(varInfo);

        bool isArray = TryMatch(Const.SYMBOL_LEFT_BRACKET, ETokenType.Symbol);
        if (isArray)
        {
            PrehandleArray(segment, varInfo);
        }

        MatchSymbol(Const.SYMBOL_EQUAL);
        CompileGammer(Grammer.Expression);
        MatchSymbol(Const.SYMBOL_SEMICOLON);

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

    #endregion

    #region utils

    SubroutineSymbol getCurrentSubroutineSymbol() => CurrentSymbol as SubroutineSymbol ?? throw new ArgumentNullException(nameof(CurrentSymbol));
    ClassSymbol getCurrentClassSymbol() => symbolStack.Peek(1) as ClassSymbol ?? throw new ArgumentNullException(nameof(CurrentSymbol));

    string encodeFunctionName(string className, string name) => className + "." + name;

    void EnableBuffer()
    {
        buffer = stringBuilder;
        buffer.Clear();
    }

    string DisableBuffer(bool isAutoWrite = false)
    {
        if (buffer == null)
        {
            return string.Empty;
        }

        string str = buffer.ToString();

        buffer = null;

        if (isAutoWrite)
        {
            Write(str);
        }

        return str;
    }


    protected override void Write(string str)
    {
        if (buffer != null)
        {
            buffer.Append(str);
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

    void calExpression()
    {
        while (!expressionStack.Empty())
        {
            var op = expressionStack.Peek();
            if (op == Const.SYMBOL_LEFT_PARENTHESES)
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
            segment = MEM_SEGMENT_ARGRUMENT;
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
        WriteCommand(ECommandType.C_PUSH, segment, varInfo.Index.ToString());

        MatchSymbol(Const.SYMBOL_LEFT_BRACKET);
        CompileGammer(Grammer.Expression);
        MatchSymbol(Const.SYMBOL_RIGHT_BRACKET);

        WriteCommand(ECommandType.C_ARITHMETIC, "add", string.Empty);

        // that = var[index]
        WriteCommand(ECommandType.C_POP, MEM_SEGMENT_POINTER, "1");
    }

    #endregion
}