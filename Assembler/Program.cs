// See https://aka.ms/new-console-template for more information

if (args.Length <= 0)
{
    Console.WriteLine("no asm file");
    return;
}

var assmbler = new Hack.Assembler();
assmbler.Assemble(args[0]);
