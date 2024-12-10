namespace IR2IL;

public static class Program
{
    public static void Main(string[] args)
    {
        Compiler.Compile(args[0], args[1]);
    }
}