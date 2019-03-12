namespace CLILL
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var compiler = new Compiler())
            using (var source = LLVMSourceCode.FromFile(args[0]))
            {
                compiler.Compile(source, args[1]);
            }
        }
    }
}
