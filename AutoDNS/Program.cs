namespace AutoDNS
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0) args = [""];

            try
            {
                switch (args[0])
                {
                    case "add":
                        Commands.Add();
                        break;
                    case "list":
                    case "ls":
                        Commands.List();
                        break;
                    case "remove":
                    case "rm":
                        Commands.Remove(args);
                        break;
                    case "delay":
                        Commands.Delay(args);
                        break;
                    default:
                        Console.WriteLine("Available commands: [add|list|remove|delay]");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
