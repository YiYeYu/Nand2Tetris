
namespace Jack;

public class NewEngine : TreeEngine, ICompilationEngine
{
    Code code = null!;

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

    public NewEngine()
    {
        OnStart += __onStart;
        OnEnd += __onEnd;

        OnConsume += __onConsume;
    }

    #region event handler

    void __onStart(object? sender, EventArgs e)
    {
        code = new Code(writer);
    }

    void __onEnd(object? sender, EventArgs e)
    {
        Root?.Visit(new TreeNode.Visitor
        {
            DVisitDown = VisitDown,
            DVisit = Visit,
            DVisitUp = VisitUp,
        });
    }

    void __onConsume(object? sender, ConsumeEventArgs e)
    {
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
    }

    #endregion

    #region EngineBase


    #endregion

    #region utils

    SubroutineSymbol getCurrentSubroutineSymbol() => CurrentSymbol as SubroutineSymbol ?? throw new ArgumentNullException(nameof(CurrentSymbol));
    ClassSymbol getCurrentClassSymbol() => symbolStack.Peek(1) as ClassSymbol ?? throw new ArgumentNullException(nameof(CurrentSymbol));

    string encodeFunctionName(string className, string name) => className + "." + name;

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

    void CalExpression()
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

    #endregion

    #region visitor

    void VisitDown(TreeNode node)
    {
        switch (node.Grammer)
        {

            default:
                break;
        }
    }

    void Visit(TreeNode node)
    {
        switch (node.Grammer)
        {
            case Grammer.Symbol:
                if (node?.Parent?.Grammer == Grammer.Term)
                {
                    switch (node.Token)
                    {
                        case Const.SYMBOL_LEFT_PARENTHESES:
                            expressionStack.Push(node.Token);
                            break;
                        case Const.SYMBOL_RIGHT_PARENTHESES:
                            CalExpression();
                            expressionStack.Pop();
                            break;
                        default:
                            break;
                    }
                }
                break;
            default:
                break;
        }
    }

    void VisitUp(TreeNode node)
    {
        switch (node.Grammer)
        {
            case Grammer.SubroutineBody:
                Depth--;
                break;
            case Grammer.IntegerConstant:
                WriteCommand(ECommandType.C_PUSH, "constant", node.Token);
                break;
            case Grammer.StringConstant:
                break;
            case Grammer.KeywordConstant:
                switch (node.Token)
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
                switch (node.Token)
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
                    var op = node.Token;
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
                CalExpression();
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
                CalExpression();
                break;
            default:
                break;
        }
    }

    #endregion
}