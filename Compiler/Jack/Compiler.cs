global using WORD = System.UInt16;

namespace Jack;

public class Compiler
{
    public const string INPUT_SUBFIX = ".jack";
    public const string INPUT_PATTERN = "*" + INPUT_SUBFIX;
    public const string OUTPUT_SUBFIX = ".vm";

    public void Compile(string path)
    {
        var fileAttr = File.GetAttributes(path);
        var isDir = (fileAttr & FileAttributes.Directory) == FileAttributes.Directory;

        DirectoryInfo dInfo = new(path);
        FileInfo fInfo = new(path);

        try
        {
            FileInfo[] fileInfos;
            if (isDir)
            {
                fileInfos = dInfo.GetFiles(INPUT_PATTERN);
            }
            else
            {
                fileInfos = new FileInfo[] { fInfo };
            }

            if (fileInfos == null || fileInfos.Length == 0)
            {
                Console.WriteLine("Compile failed: empty {0}", path);
                return;
            }

            SymbolTable symbolTable = new SymbolTable();
            CompilationEngine engine = new(symbolTable);

            __compile(fileInfos, engine);
        }
        catch (Exception e)
        {
            Console.WriteLine("Compile failed: {0}", e);
        }

        Console.WriteLine("Compile success: {0}", isDir);
        return;
    }

    void __compile(FileInfo[] fileInfos, CompilationEngine engine)
    {
        foreach (var fileInfo in fileInfos)
        {
            __compile(fileInfo, engine);
        }
    }

    void __compile(FileInfo fileInfo, CompilationEngine engine)
    {
        try
        {

            using var inStream = fileInfo.OpenRead();
            using var reader = new StreamReader(inStream);

            string outputFileName = fileInfo.FullName.Replace(INPUT_SUBFIX, OUTPUT_SUBFIX);
            using var outStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(outStream);

            engine.Compile(reader, writer);
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}