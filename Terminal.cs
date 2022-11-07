namespace Akasha
{
    class Terminal
    {
        public Terminal()
        {

        }
        public void Activate()
        {
            Console.WriteLine("Terminal Activated.");
        }
    }
    class Launcher
    {
        public static void Main(string[] args)
        {
            if (args.Contains("-s"))
            {
                var s = new Host();
                s.Activate();
            }
            else
            {
                var t = new Terminal();
                t.Activate();
            }
        }
    }
}