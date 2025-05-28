using System.Text.Json;

namespace AutoDNS
{
    partial class Commands
    {
        public static void Add()
        {
            Settings? settings = Utils.ReadSettings();

            if (!Utils.IsElevated())
            {
                Console.WriteLine("You need to be root/admin to edit the config."); Environment.Exit(1);
            }

            if (settings != null && settings.entries.Length > 0)
            {
                int i = 1;
                Console.WriteLine("[Cloudflare Tokens]");
                Console.WriteLine($" {i}: New token");
                foreach (var entry in settings.entries) Console.WriteLine($" {++i}: {entry.api_token}");
                Console.Write("Select a token: ");

                var tokenChoice = Utils.SelectFromList(i);
                Console.WriteLine(tokenChoice + "\n");

                if (tokenChoice != 1) Setup(settings.entries[tokenChoice - 2].api_token, settings);
            }

            while (true)
            {
                Console.Write("Insert Cloudflare API Token: ");
                var token = Console.ReadLine() ?? "";

                var verifyRequest = Utils.VerifyToken(token);
                if (Utils.HandleResponse(verifyRequest, out JsonElement? _, "Failed to verify Cloudflare token.")) Setup(token, settings);
            }
        }
        private static void Setup(string token, Settings? settings)
        {
            Console.WriteLine();

            //Zones
            var zonesRequest = Utils.GetZones(token);

            if (!Utils.HandleResponse(zonesRequest, out JsonElement? _zonesResponse, "Failed to get Cloudflare zones.")) Environment.Exit(1);
            var zones = _zonesResponse!.Value.GetProperty("result");

            //Prompt for zone selection
            int i = 0;
            Console.WriteLine("[Cloudflare Zones]");
            foreach (var zone in zones.EnumerateArray()) Console.WriteLine($" {++i}: {zone.GetProperty("name").GetString()}");
            Console.Write("Select a zone: ");

            var zoneChoice = Utils.SelectFromList(i);
            var zoneId = zones[zoneChoice - 1].GetProperty("id").GetString()!;
            Console.WriteLine(zoneChoice + "\n");

            //Records
            var recordsRequest = Utils.GetRecords(token, zoneId);

            if (!Utils.HandleResponse(recordsRequest, out JsonElement? _recordsResponse, "Failed to get Cloudflare records.")) Environment.Exit(1);
            var records = _recordsResponse!.Value.GetProperty("result");
            var filteredRecords = records.EnumerateArray().Where(r => r.GetProperty("type").GetString() is "AAAA" or "A").ToArray();

            //Prompt for record selection
            i = 0;
            Console.WriteLine("[Cloudflare Records]");
            foreach (var record in filteredRecords)
            {
                var proxied = record.GetProperty("proxied").GetBoolean();
                Console.WriteLine($" {++i}: [{record.GetProperty("type").GetString()}] {record.GetProperty("name").GetString()}{(proxied ? "*" : "")} ({record.GetProperty("content").GetString()})");
            }
            Console.Write("Select a record: ");

            var recordChoice = Utils.SelectFromList(i);
            var recordId = filteredRecords[recordChoice - 1].GetProperty("id").GetString()!;
            Console.WriteLine(recordChoice + "\n");

            //Final operation
            settings = Utils.EnsureSettings(settings);
            bool recordExists = settings.entries.Any(e => e.zones.Any(z => z.records.Contains(recordId)));

            if (recordExists) Console.WriteLine($"Record {recordId} already added.");
            else
            {
                AddToSettings(token, zoneId, recordId, settings);
                Console.WriteLine($"Record {recordId} added successfully.");
            }
            Environment.Exit(0);
        }
        private static void AddToSettings(string token, string zoneId, string recordId, Settings settings) //Adds a record and everything that might be missing
        {
            var entry = settings.entries.FirstOrDefault(e => e.api_token == token);
            if (entry == default) entry = new Entry { api_token = token, zones = [] };

            var zone = entry.zones.FirstOrDefault(e => e.id == zoneId);
            if (zone == default) zone = new Zone { id = zoneId, records = [] };

            //Updating the zone
            var updatedRecords = zone.records
                .Where(e => e != recordId)
                .Append(recordId);

            zone = zone with { records = updatedRecords.ToArray() };

            //Updating the entry
            var updatedZones = entry.zones
                .Where(e => e.id != zone.id)
                .Append(zone);

            entry = entry with { zones = updatedZones.ToArray() };

            //Updating the settings
            var updatedEntries = settings.entries
                .Where(e => e.api_token != entry.api_token)
                .Append(entry);

            settings = settings with { entries = updatedEntries.ToArray() };

            Utils.WriteSettings(settings); //Writing changes
        }
    }
}
