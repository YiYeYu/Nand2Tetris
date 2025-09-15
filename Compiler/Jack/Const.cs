namespace Jack;

public static class Const
{
    public const string SYMBOL_LEFT_BRACE = "{";
    public const string SYMBOL_RIGHT_BRACE = "}";
    public const string SYMBOL_LEFT_PARENTHESES = "(";
    public const string SYMBOL_RIGHT_PARENTHESES = ")";
    public const string SYMBOL_LEFT_BRACKET = "[";
    public const string SYMBOL_RIGHT_BRACKET = "]";
    public const string SYMBOL_COMMA = ",";
    public const string SYMBOL_DOT = ".";
    public const string SYMBOL_SEMICOLON = ";";
    public const string SYMBOL_ADD = "+";
    public const string SYMBOL_SUB = "-";
    public const string SYMBOL_NOT = "~";
    public const string SYMBOL_MULTIPLY = "*";
    public const string SYMBOL_DIVIDE = "/";
    public const string SYMBOL_AND = "&";
    public const string SYMBOL_OR = "|";
    public const string SYMBOL_LESS_THAN = "<";
    public const string SYMBOL_GREATER_THAN = ">";
    public const string SYMBOL_EQUAL = "=";
    public const string SYMBOL_NEW_LINE = "\n";

    public const string KEYWORD_CLASS = "class";
    public const string KEYWORD_METHOD = "method";
    public const string KEYWORD_FUNCTION = "function";
    public const string KEYWORD_CONSTRUCTOR = "constructor";
    public const string KEYWORD_INT = "int";
    public const string KEYWORD_BOOLEAN = "boolean";
    public const string KEYWORD_CHAR = "char";
    public const string KEYWORD_VOID = "void";
    public const string KEYWORD_VAR = "var";
    public const string KEYWORD_STATIC = "static";
    public const string KEYWORD_FIELD = "field";
    public const string KEYWORD_LET = "let";
    public const string KEYWORD_DO = "do";
    public const string KEYWORD_IF = "if";
    public const string KEYWORD_ELSE = "else";
    public const string KEYWORD_WHILE = "while";
    public const string KEYWORD_RETURN = "return";
    public const string KEYWORD_TRUE = "true";
    public const string KEYWORD_FALSE = "false";
    public const string KEYWORD_NULL = "null";
    public const string KEYWORD_THIS = "this";

    public static readonly string[] BinaryOps = { SYMBOL_ADD, SYMBOL_SUB, SYMBOL_MULTIPLY, SYMBOL_DIVIDE, SYMBOL_AND, SYMBOL_OR, SYMBOL_LESS_THAN, SYMBOL_GREATER_THAN, SYMBOL_EQUAL };
    public static readonly string[] UnaryOps = { SYMBOL_SUB, SYMBOL_NOT };

    public static readonly string[] KeywordConstants = { KEYWORD_TRUE, KEYWORD_FALSE, KEYWORD_NULL, KEYWORD_THIS };

}

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
    ClassName,
    SubroutineName,
    VarName,

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
    IntegerConstant,
    StringConstant,
}

public static class GrammerExtension
{
    static readonly Dictionary<Grammer, string> _grammer = new()
    {
        //
        { Grammer.Class, "'class' ClassName '{' ClassVarDec* SubroutineDec* '}'"},
        { Grammer.ClassVarDec, "('static'|'field') Type VarName (',' VarName)* ';'"},
        { Grammer.Type, "'int' | 'char' | 'boolean' | ClassName"},
        { Grammer.SubroutineDec, "('constructor'|'function'|'method') ('void' | Type) SubroutineName '(' ParameterList ')' SubroutineBody"},
        { Grammer.ParameterList, "(Type VarName (',' Type VarName)*)?"},
        { Grammer.SubroutineBody, "'{' VarDec* Statements '}'"},
        { Grammer.VarDec, "'var' Type VarName (',' VarName)* ';'"},

        { Grammer.ClassName, "Identifier"},
        { Grammer.SubroutineName, "Identifier"},
        { Grammer.VarName, "Identifier"},

        //
        { Grammer.Statements, "Statement*"},
        { Grammer.Statement, "LetStatement | IfStatement | WhileStatement | DoStatement | ReturnStatement"},
        { Grammer.LetStatement, "'let' VarName ('[' Expression ']')? '=' Expression ';'"},
        { Grammer.IfStatement, "'if' '(' Expression ')' '{' Statements '}' ('else' '{' Statement '}')?"},
        { Grammer.WhileStatement, "'while' '(' Expression ')' '{' Statements '}'"},
        { Grammer.DoStatement, "'do' SubroutineCall ';'"},
        { Grammer.ReturnStatement, "'return' Expression? ';'"},

        //
        { Grammer.Expression, "Term (BinaryOp Term)*"},
        { Grammer.Term, "IntegerConstant | StringConstant | KeywordConstant | VarName | VarName '[' Expression ']' | SubroutineCall | '(' Expression ')' | UnaryOp Term"},
        { Grammer.SubroutineCall, "SubroutineName '(' ExpressionList ')'"},
        { Grammer.ExpressionList, "(Expression (',' Expression)*)?"},
        { Grammer.BinaryOp, "'+' | '-' | '*' | '/' | '&' | '|' | '<' | '>' | '='"},
        { Grammer.UnaryOp, "'-' | '~'"},
        { Grammer.KeywordConstant, "'true' | 'false' | 'null' | 'this'"},

        //
        { Grammer.Identifier, "[a-zA-Z_][a-zA-Z0-9_]*"},
        { Grammer.IntegerConstant, "[0-9]+"},
        { Grammer.StringConstant, "\"[^\"]*\""},
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

public static class TokenTypeExtension
{
    static readonly Dictionary<ETokenType, string> _symbol = new()
    {
        { ETokenType.Keyword, "keyword" },
        { ETokenType.Symbol, "symbol" },
        { ETokenType.Identifier, "identifier" },
        { ETokenType.IntegerConstant, "integerConstant" },
        { ETokenType.StringConstant, "stringConstant" },
    };

    public static string GetString(this ETokenType tokenType)
    {
        return _symbol[tokenType];
    }
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
    This,
}

public static class KeywordExtension
{
    static readonly Dictionary<EKeyword, string> _symbol = new()
    {
        { EKeyword.Class, Const.KEYWORD_CLASS },
        { EKeyword.Method, Const.KEYWORD_METHOD },
        { EKeyword.Int, Const.KEYWORD_INT },
        { EKeyword.Function, Const.KEYWORD_FUNCTION },
        { EKeyword.Boolean, Const.KEYWORD_BOOLEAN },
        { EKeyword.Constructor, Const.KEYWORD_CONSTRUCTOR },
        { EKeyword.Char, Const.KEYWORD_CHAR },
        { EKeyword.Void, Const.KEYWORD_VOID },
        { EKeyword.Var, Const.KEYWORD_VAR },
        { EKeyword.Static, Const.KEYWORD_STATIC },
        { EKeyword.Field, Const.KEYWORD_FIELD },
        { EKeyword.Let, Const.KEYWORD_LET },
        { EKeyword.Do, Const.KEYWORD_DO },
        { EKeyword.If, Const.KEYWORD_IF },
        { EKeyword.Else, Const.KEYWORD_ELSE },
        { EKeyword.While, Const.KEYWORD_WHILE },
        { EKeyword.Return, Const.KEYWORD_RETURN },
        { EKeyword.True, Const.KEYWORD_TRUE },
        { EKeyword.False, Const.KEYWORD_FALSE },
        { EKeyword.Null, Const.KEYWORD_NULL },
        { EKeyword.This, Const.KEYWORD_THIS },
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
    Dot,
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
    NewLine,
}

public static class SymbolTypeExtension
{
    static readonly Dictionary<SymbolType, string> _symbol = new()
    {
        { SymbolType.LeftBrace, Const.SYMBOL_LEFT_BRACE },
        { SymbolType.RightBrace, Const.SYMBOL_RIGHT_BRACE },
        { SymbolType.LeftParenthesis, Const.SYMBOL_LEFT_PARENTHESES },
        { SymbolType.RightParenthesis, Const.SYMBOL_RIGHT_PARENTHESES },
        { SymbolType.LeftBracket, Const.SYMBOL_LEFT_BRACKET },
        { SymbolType.RightBracket, Const.SYMBOL_RIGHT_BRACKET },
        { SymbolType.Comma, Const.SYMBOL_COMMA },
        { SymbolType.Dot, Const.SYMBOL_DOT },
        { SymbolType.Semicolon, Const.SYMBOL_SEMICOLON },
        { SymbolType.Plus, Const.SYMBOL_ADD },
        { SymbolType.Minus, Const.SYMBOL_SUB },
        { SymbolType.Not, Const.SYMBOL_NOT },
        { SymbolType.Multiply, Const.SYMBOL_MULTIPLY },
        { SymbolType.Divide, Const.SYMBOL_DIVIDE },
        { SymbolType.And, Const.SYMBOL_AND },
        { SymbolType.Or, Const.SYMBOL_OR },
        { SymbolType.LessThan, Const.SYMBOL_LESS_THAN },
        { SymbolType.GreaterThan, Const.SYMBOL_GREATER_THAN },
        { SymbolType.Equal, Const.SYMBOL_EQUAL },
        { SymbolType.NewLine, Const.SYMBOL_NEW_LINE },
    };

    static readonly Dictionary<string, SymbolType> _string = _symbol.ToDictionary(x => x.Value, x => x.Key);
    static readonly Dictionary<char, SymbolType> _char = _symbol.ToDictionary(x => x.Value[0], x => x.Key);

    public static bool IsSymbol(char c) => _char.ContainsKey(c);
    public static SymbolType GetSymbolType(string c) => _string[c];
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