using System.Text.Json;

namespace AutoDNS
{
    partial class Commands
    {
        public static void List()
        {
            Settings? settings = Utils.ReadSettings();

            if (settings != null && settings.entries.Length > 0)
            {
                Console.WriteLine("[Cloudflare Records]");
                int i = 0;

                foreach (var entry in settings.entries)
                {
                    foreach (var zone in entry.zones)
                    {
                        var recordsRequest = Utils.GetRecords(entry.api_token, zone.id);

                        Dictionary<string, JsonElement> recordsDict = [];
                        if (Utils.HandleResponse(recordsRequest, out JsonElement? _recordsResponse, ""))
                        {
                            var recordsResponse = _recordsResponse!.Value.GetProperty("result").EnumerateArray();
                            recordsDict = recordsResponse.ToDictionary(e => e.GetProperty("id").GetString()!, e => e);
                        }

                        foreach (var r in zone.records)
                        {
                            i++;
                            if (recordsDict.TryGetValue(r, out JsonElement record))
                            {
                                var proxied = record.GetProperty("proxied").GetBoolean();
                                Console.WriteLine($" {i}: [{record.GetProperty("type").GetString()}] {record.GetProperty("name").GetString()}{(proxied ? "*" : "")} ({record.GetProperty("content").GetString()})");
                            }
                            else Console.WriteLine($" {i}: {r} (Error getting details from Cloudflare)");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("No records to list.");
                Environment.Exit(0);
            }
        }
    }
}
