namespace Hack;

public partial class VMTranslator
{
    public enum ECommandType
    {
        C_ARITHMETIC,
        C_PUSH,
        C_POP,
        C_LABEL,
        C_GOTO,
        C_IF,
        C_FUNCTION,
        C_RETURN,
        C_CALL,
    }

    public const string INPUT_SUBFIX = ".vm";
    public const string OUTPUT_SUBFIX = ".asm";
    const int BUFF_SIZE = 1024;


    public void Translate(string path)
    {
        var fInfo = new FileInfo(path);
        var outputFileName = fInfo.FullName.Replace(INPUT_SUBFIX, OUTPUT_SUBFIX);

        try
        {
            if (File.Exists(outputFileName))
            {
                File.Delete(outputFileName);
            }

            using var inStream = File.OpenRead(fInfo.FullName);
            using var reader = new StreamReader(inStream);
            var parser = new Parser(reader);

            using var outStream = File.Create(outputFileName, BUFF_SIZE);
            using var writer = new StreamWriter(outStream);
            var code = new Code(writer);

            __translate(parser, code);

            writer.Flush();
        }
        catch (Exception e)
        {
            Console.WriteLine("Translate failed: {0}", e);
        }
        finally
        {

        }

        Console.WriteLine("Translate success: {0}", outputFileName);
        return;
    }

    void __translate(Parser parser, Code code)
    {

    }
}