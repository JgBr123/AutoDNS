namespace AutoDNS
{
    partial class Commands
    {
        public static void Remove(string[] args)
        {
            Settings? settings = Utils.ReadSettings();

            if (!Utils.IsElevated())
            {
                Console.WriteLine("You need to be root/admin to edit the config."); Environment.Exit(1);
            }

            if (args.Length < 2)
            {
                Console.WriteLine("Insert an index to remove.");
                Environment.Exit(1);
            }

            if (!Int32.TryParse(args[1], out int result))
            {
                Console.WriteLine("Index is not an integer.");
                Environment.Exit(1);
            }

            if (result <= 0)
            {
                Console.WriteLine("Index must be greater than 0.");
                Environment.Exit(1);
            }

            if (settings != null && settings.entries.Length > 0)
            {
                int i = 0;

                foreach (var entry in settings.entries)
                {
                    foreach (var zone in entry.zones)
                    {
                        foreach (var r in zone.records)
                        {
                            i++;
                            if (result == i)
                            {
                                RemoveFromSettings(entry.api_token, zone.id, r, settings);
                                Console.WriteLine($"Record {r} removed successfully.");
                                Environment.Exit(0);
                            }
                        }
                    }
                }

                Console.WriteLine($"No record found with index {result}.");
                Environment.Exit(1);
            }
            else
            {
                Console.WriteLine("No records to remove.");
                Environment.Exit(0);
            }
        }
        private static void RemoveFromSettings(string token, string zoneId, string recordId, Settings settings) //Removes a record and everything else that might be empty
        {
            var entry = settings.entries.FirstOrDefault(e => e.api_token == token);
            if (entry == default) Environment.Exit(1);

            var zone = entry.zones.FirstOrDefault(e => e.id == zoneId);
            if (zone == default) Environment.Exit(1);

            //Updating the zone
            var updatedRecords = zone.records.Where(e => e != recordId);
            zone = zone with { records = updatedRecords.ToArray() };

            //Updating the entry
            var updatedZones = entry.zones.Where(e => e.id != zone.id);
            if (zone.records.Length > 0) updatedZones = updatedZones.Append(zone);

            entry = entry with { zones = updatedZones.ToArray() };

            //Updating the settings
            var updatedEntries = settings.entries.Where(e => e.api_token != entry.api_token);
            if (entry.zones.Length > 0) updatedEntries = updatedEntries.Append(entry);

            settings = settings with { entries = updatedEntries.ToArray() };

            Utils.WriteSettings(settings); //Writing changes
        }
    }
}
