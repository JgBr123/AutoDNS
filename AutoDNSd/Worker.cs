using System.Runtime.InteropServices;
using System.Text.Json;

namespace AutoDNSd
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private Settings? settings;

        public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            Utils.httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Service started.");
            LoadConfig();

            if (settings == null) await Task.Delay(-1, stoppingToken); //Waits until reload

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Loop();
                    await Task.Delay(TimeSpan.FromSeconds(settings!.delay), stoppingToken); //Runs with the configured delay
                }
                catch (Exception ex) { logger.LogCritical(ex.Message); }
            }
        }
        private void Loop()
        {
            var ipv4 = Utils.GetPublicIPv4();
            var ipv6 = Utils.GetPublicIPv6();

            foreach (var entry in settings!.entries)
            {
                var token = entry.api_token;
                foreach (var zone in entry.zones)
                {
                    var zoneId = zone.id;

                    //Request to cloudflare getting all records from the zone at once
                    var request = Utils.GetAllRecords(token, zoneId);

                    if (!HandleResponse(request, out JsonElement? _recordsResponse, "Failed to get DNS records from Cloudflare.")) continue;
                    var recordsResponse = _recordsResponse!.Value;

                    //Doing stuff with the response of the request
                    foreach (var record in recordsResponse.GetProperty("result").EnumerateArray())
                    {
                        var recordId = record.GetProperty("id").GetString()!;
                        var recordType = record.GetProperty("type").GetString()!;
                        var recordContent = record.GetProperty("content").GetString()!;

                        if (zone.records.Contains(recordId))
                        {
                            if (recordType != "AAAA" && recordType != "A")
                            {
                                logger.LogWarning("The DNS record is not of type AAAA or A. [{details}]", "ID " + recordId); continue;
                            }

                            var ip = recordType == "AAAA" ? ipv6 : ipv4;
                            var ipType = recordType == "AAAA" ? "IPv6" : "IPv4";

                            if (ip == null) //Error if there is no ipv4 or ipv6 when its needed
                            {
                                logger.LogError("Failed to update DNS record content. [{details}]", $"Failed to get public {ipType} address"); continue;
                            }
                            else if (recordContent == ip) continue; //Ignore if the ip didnt change

                            //Request to cloudflare editing the record with the new ip
                            var patchRequest = Utils.UpdateRecord(token, zoneId, recordId, recordType, ip);

                            if (!HandleResponse(patchRequest, out JsonElement? _, "Failed to update DNS record content.")) continue;
                            else logger.LogInformation("The DNS record was updated from {old_ip} to {new_ip}. [{details}]", recordContent, ip, "ID " + recordId);
                        }
                    }
                }
            }
        }
        private bool HandleResponse(Task<HttpResponseMessage> request, out JsonElement? response, string errorMessageBase) //Returns true if succesfull, false if errors
        {
            response = null;

            try { request.Wait(); }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                if (errorMessageBase != null) logger.LogError(errorMessageBase + " [{details}]", errorMessage);
                return false;
            }

            if (!request.IsCompletedSuccessfully)
            {
                var errorMessage = request.Exception?.InnerException?.Message ?? request.Exception?.Message ?? "No details";
                if (errorMessageBase != null) logger.LogError(errorMessageBase + " [{details}]", errorMessage);
                return false;
            }

            var json = request.Result.Content.ReadAsStringAsync().Result;
            var res = JsonDocument.Parse(json).RootElement;

            if (!request.Result.IsSuccessStatusCode || !res.GetProperty("success").GetBoolean())
            {
                var errors = res.GetProperty("errors");
                var errorMessage = errors.GetArrayLength() > 0 ? errors[0].GetProperty("message").GetString() : $"HTTP {request.Result.StatusCode}";
                if (errorMessageBase != null) logger.LogError(errorMessageBase + " [{details}]", errorMessage);
                return false;
            }

            response = res;
            return true;
        }
        private void LoadConfig()
        {
            var baseFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/etc/opt" : Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var serviceFolder = Path.Combine(baseFolder, "AutoDNS");
            var configFile = Path.Combine(serviceFolder, "config.json");

            if (!File.Exists(configFile))
            {
                logger.LogWarning("Configuration file not found. Creating an empty one.");
                try
                {
                    var settings = new Settings { delay = 60, entries = [] };

                    Directory.CreateDirectory(serviceFolder);
                    File.WriteAllText(configFile, JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings));
                    logger.LogWarning($"Configuration file created at {configFile}. You have to configure it manually for the service to work.");
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.InnerException?.Message ?? ex.Message;
                    logger.LogError("Error while trying to create the configuration file. [{details}]", errorMessage);
                }
            }
            else
            {
                try
                {
                    var json = File.ReadAllText(configFile);
                    settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings) ?? throw new Exception();
                }
                catch (Exception ex) { logger.LogCritical("Error while trying to load the configuration file. [{details}]", ex.Message); }
            }
        }
    }
}
