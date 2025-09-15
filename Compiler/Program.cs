// See https://aka.ms/new-console-template for more information

if (args.Length <= 0)
{
    Console.WriteLine("no file");
    return;
}

var compiler = new Jack.Compiler();
compiler.Compile(args[0], args?[1]);
