namespace AutoDNSd
{
    class Utils
    {
        public static IHttpClientFactory httpClientFactory = null!;
        public static string? GetPublicIPv4()
        {
            try { return httpClientFactory.CreateClient().GetStringAsync("https://api.ipify.org").Result; }
            catch { return null; }
        }
        public static string? GetPublicIPv6()
        {
            try { return httpClientFactory.CreateClient().GetStringAsync("https://api6.ipify.org").Result; }
            catch { return null; }
        }
        public static Task<HttpResponseMessage> GetAllRecords(string auth_token, string zone_id)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.cloudflare.com/client/v4/zones/{zone_id}/dns_records");
            request.Headers.Add("Authorization", auth_token);
            return httpClientFactory.CreateClient().SendAsync(request);
        }
        public static Task<HttpResponseMessage> UpdateRecord(string auth_token, string zone_id, string record_id, string record_type, string ip)
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"https://api.cloudflare.com/client/v4/zones/{zone_id}/dns_records/{record_id}");
            request.Headers.Add("Authorization", auth_token);
            request.Content = new StringContent("{" + @$"""content"": ""{ip}"",""type"": ""{record_type}""" + "}");
            return httpClientFactory.CreateClient().SendAsync(request);
        }
    }
}
