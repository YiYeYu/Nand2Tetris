using System.Diagnostics;
using System.Text;

namespace Jack;

public interface ICompilationEngine
{
    void Compile(StreamReader reader, StreamWriter writer);

    void CompileGammer(Grammer grammer);
}