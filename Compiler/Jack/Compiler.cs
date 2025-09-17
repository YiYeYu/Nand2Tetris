global using WORD = System.UInt16;

namespace Jack;

public class Compiler
{
    public const string INPUT_SUBFIX = ".jack";
    public const string INPUT_PATTERN = "*" + INPUT_SUBFIX;
    public const string OUTPUT_SUBFIX = ".vm";

    public void Compile(string path, string? engineName = nameof(Engine))
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

            Type compilationEngineType = Type.GetType($"Jack.{engineName}") ?? throw new Exception($"Engine not found: {engineName}");

            ICompilationEngine engine = (ICompilationEngine?)Activator.CreateInstance(compilationEngineType) ?? throw new Exception($"Engine not found: {engineName}");

            __compile(fileInfos, engine);
        }
        catch (Exception e)
        {
            Console.WriteLine("Compile failed: {0}, {1}", e.Data["file"], e);
        }

        Console.WriteLine("Compile success: {0}", isDir);
        return;
    }

    void __compile(FileInfo[] fileInfos, ICompilationEngine engine)
    {
        foreach (var fileInfo in fileInfos)
        {
            __compile(fileInfo, engine);
        }
    }

    void __compile(FileInfo fileInfo, ICompilationEngine engine)
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
        catch (System.Exception e)
        {
            e.Data["file"] = fileInfo.FullName;
            throw;
        }
    }
}