using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;

namespace AutoDNS
{
    class Utils
    {
        public static readonly string baseFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/etc/opt" : Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        public static readonly string serviceFolder = Path.Combine(baseFolder, "AutoDNS");
        public static readonly string configFile = Path.Combine(serviceFolder, "config.json");

        private static readonly HttpClient httpClient = new();
        public static Task<HttpResponseMessage> VerifyToken(string auth_token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.cloudflare.com/client/v4/user/tokens/verify");
            request.Headers.Add("Authorization", auth_token);
            return httpClient.SendAsync(request);
        }
        public static Task<HttpResponseMessage> GetZones(string auth_token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.cloudflare.com/client/v4/zones");
            request.Headers.Add("Authorization", auth_token);
            return httpClient.SendAsync(request);
        }
        public static Task<HttpResponseMessage> GetRecords(string auth_token, string zone_id)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.cloudflare.com/client/v4/zones/{zone_id}/dns_records");
            request.Headers.Add("Authorization", auth_token);
            return httpClient.SendAsync(request);
        }
        public static bool HandleResponse(Task<HttpResponseMessage> request, out JsonElement? response, string? errorMessageBase) //Returns true if succesfull, false if errors
        {
            response = null;

            try { request.Wait(); }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                if (errorMessageBase != null) Console.WriteLine(errorMessageBase + $" [{errorMessage}]");
                return false;
            }

            if (!request.IsCompletedSuccessfully)
            {
                var errorMessage = request.Exception?.InnerException?.Message ?? request.Exception?.Message ?? "No details";
                if (errorMessageBase != null) Console.WriteLine(errorMessageBase + $" [{errorMessage}]");
                return false;
            }

            var json = request.Result.Content.ReadAsStringAsync().Result;
            var res = JsonDocument.Parse(json).RootElement;

            if (!request.Result.IsSuccessStatusCode || !res.GetProperty("success").GetBoolean())
            {
                var errors = res.GetProperty("errors");
                var errorMessage = errors.GetArrayLength() > 0 ? errors[0].GetProperty("message").GetString() : $"HTTP {request.Result.StatusCode}";
                if (errorMessageBase != null) Console.WriteLine(errorMessageBase + $" [{errorMessage}]");
                return false;
            }

            response = res;
            return true;
        }
        public static int SelectFromList(int maxOption)
        {
            while (true)
            {
                var key = Console.ReadKey(true).KeyChar;
                if (int.TryParse(key.ToString(), out int choice) && choice > 0 && choice <= maxOption)
                {
                    return choice;
                }
            }
        }
        public static Settings? ReadSettings()
        {
            if (File.Exists(configFile))
            {
                try
                {
                    var json = File.ReadAllText(Utils.configFile);
                    return JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings) ?? throw new Exception();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while trying to load the configuration file. [{ex.Message}]"); Environment.Exit(1);
                }
            }
            return null;
        }
        public static void WriteSettings(Settings settings)
        {
            Directory.CreateDirectory(serviceFolder);
            File.WriteAllText(configFile, JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings));
        }
        public static Settings EnsureSettings(Settings? settings)
        {
            if (settings == null) return new Settings { delay = 60, entries = [] };
            else return settings;
        }
        public static bool IsElevated()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
                return IsAdmin();
            else 
                return getuid() == 0;
        }
        private static bool IsAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        [DllImport("libc")]
        public static extern uint getuid();
    }
}
