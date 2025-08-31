namespace Hack;

public partial class VMTranslator
{
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

    public static readonly Dictionary<string, ECommandType> commandMap = new()
    {
        {"call", ECommandType.C_CALL},
        {"function", ECommandType.C_FUNCTION},
        {"goto", ECommandType.C_GOTO},
        {"if-goto", ECommandType.C_IF},
        {"label", ECommandType.C_LABEL},
        {"pop", ECommandType.C_POP},
        {"push", ECommandType.C_PUSH},
        {"return", ECommandType.C_RETURN},

        {"add", ECommandType.C_ARITHMETIC},
        {"sub", ECommandType.C_ARITHMETIC},
        {"neg", ECommandType.C_ARITHMETIC},
        {"eq", ECommandType.C_ARITHMETIC},
        {"gt", ECommandType.C_ARITHMETIC},
        {"lt", ECommandType.C_ARITHMETIC},
        {"and", ECommandType.C_ARITHMETIC},
        {"or", ECommandType.C_ARITHMETIC},
        {"not", ECommandType.C_ARITHMETIC},
    };

    public static readonly Dictionary<string, ECommandType> arithmeticCommandMap = new()
    {
        {"add", ECommandType.C_ARITHMETIC},
        {"sub", ECommandType.C_ARITHMETIC},
        {"neg", ECommandType.C_ARITHMETIC},
        {"eq", ECommandType.C_ARITHMETIC},
        {"gt", ECommandType.C_ARITHMETIC},
        {"lt", ECommandType.C_ARITHMETIC},
        {"and", ECommandType.C_ARITHMETIC},
        {"or", ECommandType.C_ARITHMETIC},
        {"not", ECommandType.C_ARITHMETIC},
    };

    public const string INPUT_SUBFIX = ".vm";
    public const string OUTPUT_SUBFIX = ".asm";

    public void Translate(string path)
    {
        var fileAttr = File.GetAttributes(path);
        var isDir = (fileAttr & FileAttributes.Directory) == FileAttributes.Directory;

        DirectoryInfo dInfo = new(path);
        FileInfo fInfo = new(path);

        string outputFileName;
        if (isDir)
        {
            outputFileName = Path.Combine(path, dInfo.Name + OUTPUT_SUBFIX);
        }
        else
        {
            outputFileName = fInfo.FullName.Replace(INPUT_SUBFIX, OUTPUT_SUBFIX);
        }

        try
        {
            using var outStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(outStream);
            var code = new Code(writer);

            if (isDir)
            {
                foreach (var fileInfo in dInfo.GetFiles(INPUT_SUBFIX))
                {
                    __translate(fileInfo, code);
                }
            }
            else
            {
                __translate(fInfo, code);
            }

            writer.Flush();
        }
        catch (Exception e)
        {
            Console.WriteLine("Translate failed: {0}", e);
        }

        Console.WriteLine("Translate success: {0}", outputFileName);
        return;
    }

    void __translate(FileInfo fileInfo, Code code)
    {
        try
        {
            using var inStream = fileInfo.OpenRead();
            using var reader = new StreamReader(inStream);
            var parser = new Parser(reader);

            code.SetFile(Path.GetFileNameWithoutExtension(fileInfo.Name));
            __translate(parser, code);
            code.CloseFile();
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    void __translate(Parser parser, Code code)
    {
        try
        {
            while (parser.HasMoreCommands())
            {
                parser.Advandce();

                code.WriteCommand
                (
                    parser.CommandType(),
                    parser.Arg1(),
                    parser.Arg2()
                );
            }

        }
        catch (System.Exception e)
        {
            throw new Exception(
                string.Format(
                    "line: {0}, command: {1}, type: {2}, buffer: {3}; arg1: {4}, arg2: {5}",
                    parser.CurrentLine,
                    parser.CurrentCommand,
                    parser.CommandType(),
                    parser.Buffer,
                    parser.Arg1(),
                    parser.Arg2()
                ),
                e
            );
        }
    }
}