namespace Jack;

public enum Grammer
{
    // structure
    Class,
    ClassVarDec,
    Type,
    SubroutineDec,
    ParameterList,
    SubroutineBody,
    VarDec,

    // statements
    Statements,
    Statement,
    LetStatement,
    IfStatement,
    WhileStatement,
    DoStatement,
    ReturnStatement,

    // expressions
    Expression,
    Term,
    SubroutineCall,
    ExpressionList,
    BinaryOp,
    UnaryOp,
    KeywordConstant,

    // identifier
    Identifier,
}

public static class GrammerExtension
{
    static readonly Dictionary<Grammer, string> _grammer = new()
    {
        { Grammer.Class, "'class' Identifier '{' ClassVarDec* SubroutineDec* '}'"},
        { Grammer.ClassVarDec, "('static'|'field') Type Identifier (',' Identifier)* ';'"},
        { Grammer.Type, "'int' | 'char' | 'boolean' | Identifier"},
        { Grammer.SubroutineDec, "('constructor'|'function'|'method') ('void' | Type) SubroutineName '(' ParameterList ')' SubroutineBody"},
        { Grammer.ParameterList, "(Type Identifier (',' Type Identifier)*)?"},
        { Grammer.SubroutineBody, "'{' VarDec* Statements '}'"},
        { Grammer.VarDec, "'var' Type Identifier (',' Identifier)* ';'"},
        { Grammer.Statements, "Statement*"},
        { Grammer.Statement, "LetStatement | IfStatement | WhileStatement | DoStatement | ReturnStatement"},
        { Grammer.LetStatement, "'let' Identifier ('[' Expression ']')? '=' Expression ';'"},
        { Grammer.IfStatement, "'if' '(' Expression ')' '{' Statements '}' ('else' '{' Statement '}')?"},
        { Grammer.WhileStatement, "'while' '(' Expression ')' '{' Statements '}'"},
        { Grammer.DoStatement, "'do' SubroutineCall ';'"},
        { Grammer.ReturnStatement, "'return' Expression? ';'"},
        { Grammer.Expression, "Term (BinaryOp Term)*"},
        { Grammer.Term, "IntegerConstant | StringConstant | KeywordConstant | Identifier | Identifier '[' Expression ']' | SubroutineCall | '(' Expression ')' | UnaryOp Term"},
        { Grammer.SubroutineCall, "Identifier '(' ExpressionList ')'"},
        { Grammer.ExpressionList, "(Expression (',' Expression)*)?"},
        { Grammer.BinaryOp, "'+' | '-' | '*' | '/' | '&' | '|' | '<' | '>' | '='"},
        { Grammer.UnaryOp, "'-' | '~'"},
        { Grammer.KeywordConstant, "'true' | 'false' | 'null' | 'this'"},
        { Grammer.Identifier, "[a-zA-Z_][a-zA-Z0-9_]*"},
    };

    public static string GetString(this Grammer grammer) => _grammer[grammer];
}

public enum ETokenType
{
    Keyword,
    Symbol,
    Identifier,
    IntegerConstant,
    StringConstant,
}

public enum EKeyword
{
    Class,
    Method,
    Int,
    Function,
    Boolean,
    Constructor,
    Char,
    Void,
    Var,
    Static,
    Field,
    Let,
    Do,
    If,
    Else,
    While,
    Return,
    True,
    False,
    Null,
}

public static class KeywordExtension
{
    static readonly Dictionary<EKeyword, string> _symbol = new()
    {
        { EKeyword.Class, "class" },
        { EKeyword.Method, "method" },
        { EKeyword.Int, "int" },
        { EKeyword.Function, "function" },
        { EKeyword.Boolean, "boolean" },
        { EKeyword.Constructor, "constructor" },
        { EKeyword.Char, "char" },
        { EKeyword.Void, "void" },
        { EKeyword.Var, "var" },
        { EKeyword.Static, "static" },
        { EKeyword.Field, "field" },
        { EKeyword.Let, "let" },
        { EKeyword.Do, "do" },
        { EKeyword.If, "if" },
        { EKeyword.Else, "else" },
        { EKeyword.While, "while" },
        { EKeyword.Return, "return" },
        { EKeyword.True, "true" },
        { EKeyword.False, "false" },
        { EKeyword.Null, "null" },
    };
    static readonly Dictionary<string, EKeyword> _symbolReverse = _symbol.ToDictionary(x => x.Value, x => x.Key);

    public static string GetString(this EKeyword keyword) => _symbol[keyword];

    public static bool IsKeyword(this string keyword) => _symbolReverse.ContainsKey(keyword);
    public static EKeyword GetKeyword(this string keyword) => _symbolReverse[keyword];
}

public enum SymbolType
{
    LeftBrace,
    RightBrace,
    LeftParenthesis,
    RightParenthesis,
    LeftBracket,
    RightBracket,
    Comma,
    Period,
    Semicolon,
    Plus,
    Minus,
    Not,
    Multiply,
    Divide,
    And,
    Or,
    LessThan,
    GreaterThan,
    Equal,
}

public static class SymbolTypeExtension
{
    static readonly Dictionary<SymbolType, string> _symbol = new()
    {
        { SymbolType.LeftBrace, "{" },
        { SymbolType.RightBrace, "}" },
        { SymbolType.LeftParenthesis, "(" },
        { SymbolType.RightParenthesis, ")" },
        { SymbolType.LeftBracket, "[" },
        { SymbolType.RightBracket, "]" },
        { SymbolType.Comma, "," },
        { SymbolType.Period, "." },
        { SymbolType.Semicolon, ";" },
        { SymbolType.Plus, "+" },
        { SymbolType.Minus, "-" },
        { SymbolType.Not, "~" },
        { SymbolType.Multiply, "*" },
        { SymbolType.Divide, "/" },
        { SymbolType.And, "&" },
        { SymbolType.Or, "|" },
        { SymbolType.LessThan, "<" },
        { SymbolType.GreaterThan, ">" },
        { SymbolType.Equal, "=" },
    };

    static readonly Dictionary<char, SymbolType> _char = _symbol.ToDictionary(x => x.Value[0], x => x.Key);


    public static string GetString(this SymbolType symbol) => _symbol[symbol];
    public static char GetChar(this SymbolType symbol) => GetString(symbol)[0];

    public static bool IsSymbol(char c) => _char.ContainsKey(c);
    public static SymbolType GetSymbolType(char c) => _char[c];
}

public enum ECommandType
{
    /// <summary>
    /// 算术逻辑指令<br/>x-y-SP<br>
    /// add: x + y<br/>
    /// sub: x - y<br/>
    /// neg: - y<br/>
    /// eq: x == y<br/>
    /// gt: x > y<br/>
    /// lt: x < y<br/>
    /// and: x & y<br/>
    /// or: x | y<br/>
    /// not: ~y<br/>
    /// </summary>
    C_ARITHMETIC,

    /// <summary>
    /// 内存访问指令push segment index<br/>
    /// 将segment[index]入栈<br/>
    /// segment: <br/>
    /// argument: 函数参数<br/>
    /// local: 函数本地变量<br/>
    /// static: 同一vm文件共享静态变量<br/>
    /// constant: 常量0x0000..0x7FFF<br/>
    /// this: <br/>
    /// that: <br/>
    /// pointer: 0->this,1->that<br/>
    /// temp: 8个共享临时变量<br/>
    /// </summary>
    C_PUSH,
    /// <summary>
    /// 内存访问指令pop segment index<br/>
    /// 栈顶出栈，存入segment[index]<br>
    /// </summary>
    C_POP,

    /// <summary>
    /// 程序流程控制label，
    /// </summary>
    C_LABEL,
    /// <summary>
    /// 程序流程控制goto label，无条件跳转
    /// </summary>
    C_GOTO,
    /// <summary>
    /// 程序流程控制if-goto label，条件跳转，弹出栈顶，非零跳转，跳转地址必须在同一函数内
    /// </summary>
    C_IF,

    /// <summary>
    /// 函数调用指令function name nLocals，函数声明，指明函数名name，本地变量数量nLocals
    /// </summary>
    C_FUNCTION,
    /// <summary>
    /// 函数调用指令call name nArgs, 函数调用，指明函数名name，参数数量nArgs
    /// </summary>
    C_CALL,
    /// <summary>
    /// 函数调用指令return
    /// </summary>
    C_RETURN,
}

public enum EArithmeticCommand
{
    ADD,
    SUB,
    NEG,
    EQ,
    GT,
    LT,
    AND,
    OR,
    NOT,
}

public static class EArithmeticCommandExtension
{
    static readonly Dictionary<EArithmeticCommand, string> _symbol = new()
    {
        { EArithmeticCommand.ADD, "add" },
        { EArithmeticCommand.SUB, "sub" },
        { EArithmeticCommand.NEG, "neg" },
        { EArithmeticCommand.EQ, "eq" },
        { EArithmeticCommand.GT, "gt" },
        { EArithmeticCommand.LT, "lt" },
        { EArithmeticCommand.AND, "and" },
        { EArithmeticCommand.OR, "or" },
        { EArithmeticCommand.NOT, "not" },
    };

    public static string GetString(this EArithmeticCommand symbol) => _symbol[symbol];
}