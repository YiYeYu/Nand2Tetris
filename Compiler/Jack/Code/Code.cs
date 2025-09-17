
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Jack;

public class Code
{
    const string ARITHMETIC_ADD = "add";
    const string ARITHMETIC_SUB = "sub";
    const string ARITHMETIC_NEG = "neg";
    const string ARITHMETIC_EQ = "eq";
    const string ARITHMETIC_GT = "gt";
    const string ARITHMETIC_LT = "lt";
    const string ARITHMETIC_AND = "and";
    const string ARITHMETIC_OR = "or";
    const string ARITHMETIC_NOT = "not";

    delegate void DWriteCommand(Code code, ECommandType cmd, string arg1, string arg2);
    static readonly Dictionary<ECommandType, DWriteCommand> WriteCommands = new(){
        {ECommandType.C_ARITHMETIC, WriteArithmetic},
        {ECommandType.C_PUSH, WritePush},
        {ECommandType.C_POP, WritePop},

        {ECommandType.C_LABEL, WriteLabel},
        {ECommandType.C_GOTO, WriteGoto},
        {ECommandType.C_IF, WriteIf},

        {ECommandType.C_FUNCTION, WriteFunction},
        {ECommandType.C_CALL, WriteCall},
        {ECommandType.C_RETURN, WriteReturn},
    };

    readonly StreamWriter writer;

    public Code(StreamWriter writer)
    {
        this.writer = writer;
    }

    ~Code()
    {
    }

    void Write(string word)
    {
        writer.Write(word);
    }

    public void WriteCommand(ECommandType cmd, string arg1, string arg2)
    {
        WriteCommands[cmd](this, cmd, arg1, arg2);
    }

    public static void WriteArithmetic(Code code, ECommandType _, string cmd, string __)
    {
        code.Write($"{cmd}\n");
    }

    /// <summary>
    /// TODO: check segment index
    /// </summary>
    /// <param name="code"></param>
    /// <param name="_"></param>
    /// <param name="segment"></param>
    /// <param name="index"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static void WritePush(Code code, ECommandType _, string segment, string index)
    {
        code.Write($"push {segment} {index}\n");
    }

    public static void WritePop(Code code, ECommandType _, string segment, string index)
    {
        code.Write($"pop {segment} {index}\n");
    }

    public static void WriteLabel(Code code, ECommandType _, string label, string __)
    {
        code.Write($"label {label}\n");
    }

    public static void WriteGoto(Code code, ECommandType _, string label, string __)
    {
        code.Write($"goto {label}\n");
    }

    public static void WriteIf(Code code, ECommandType _, string label, string __)
    {
        code.Write($"if-goto {label}\n");
    }

    public static void WriteFunction(Code code, ECommandType _, string funName, string nParams)
    {
        code.Write($"function {funName} {nParams}\n");
    }

    public static void WriteCall(Code code, ECommandType _, string funName, string nParams)
    {
        code.Write($"call {funName} {nParams}\n");
    }

    public static void WriteReturn(Code code, ECommandType _, string __, string ___)
    {
        code.Write("return\n");
    }
}