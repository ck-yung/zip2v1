namespace zip2.create
{
    internal class Command : CommandBase
    {
        public override int Invoke()
        {            
            Console.WriteLine("TODO: create.Invoke()");
            return 0;
        }

        public override bool Parse(IEnumerable<string> args)
        {
            Console.WriteLine("TODO: create.Parse()");
            foreach (var arg in args)
            {
                Console.WriteLine($"\t{arg}");
            }
            return true;
        }

        public override int SayHelp()
        {
            Console.WriteLine("TODO: create.SayHelp()");
            return 0;
        }
    }
}
