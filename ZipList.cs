namespace zip2.list
{
    internal class Command : CommandBase
    {        
        public override int Invoke()
        {            
            Console.WriteLine("TODO: list.Invoke()");
            return 0;
        }

        public override bool Parse(IEnumerable<string> args)
        {
            Console.WriteLine("TODO: list.Parse()");
            foreach (var arg in args)
            {
                Console.WriteLine($"\t{arg}");
            }
            return true;
        }

        public override int SayHelp()
        {
            Console.WriteLine("TODO: list.SayHelp()");
            return 0;
        }
    }
}
