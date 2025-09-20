
namespace Jack;

public class Engine : EngineBase, ICompilationEngine
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

    public Engine()
    {
        OnStart += __onStart;

        OnAdvance += __onAdvance;
        OnConsume += __onConsume;
        OnDefine += __onDefine;
        OnEndDefine += __onEndDefine;

        OnEnterGrammer += __onEnterGrammer;
        OnLeaveGrammer += __onLeaveGrammer;
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

    void __onEnterGrammer(object? sender, GrammerEventArgs e)
    {

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

    void __onLeaveGrammer(object? sender, GrammerEventArgs e)
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
            default:
                break;
        }
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
        WriteComment($"grammar {CurrentGrammer}, symbol {CurrentSymbol}, depth {Depth}, token '{parser.Token()}', line {parser.CurrentLine}:{parser.CurrentColumn}\n");
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

    #endregion
}