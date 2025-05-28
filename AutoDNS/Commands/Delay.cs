namespace AutoDNS
{
    partial class Commands
    {
        public static void Delay(string[] args)
        {
            Settings? settings = Utils.ReadSettings();

            if (args.Length > 1)
            {
                if (!Utils.IsElevated())
                {
                    Console.WriteLine("You need to be root/admin to edit the config."); Environment.Exit(1);
                }

                if (!Int32.TryParse(args[1], out int result))
                {
                    Console.WriteLine("Delay is not an integer.");
                    Environment.Exit(1);
                }
                if (result <= 0)
                {
                    Console.WriteLine("Delay must be greater than 0.");
                    Environment.Exit(1);
                }

                settings = Utils.EnsureSettings(settings);
                EditSettingsDelay(result, settings);
                Console.WriteLine($"The current delay is now set to {result} seconds.");
            }
            else
            {
                if (settings != null) Console.WriteLine($"The current delay is {settings.delay} seconds.");
                else Console.WriteLine("No configuration file found.");
            }
            Environment.Exit(0);
        }
        private static void EditSettingsDelay(int delay, Settings settings) //Edits the delay from settings
        {
            settings = settings with { delay = delay };
            Utils.WriteSettings(settings); //Writing changes
        }
    }
}
