using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace CovidTracker.Code
{
    /// <summary>
    /// Helper class which provides GET and POST methods to interact with the server.
    /// </summary>
    public static class AppClient
    {
        public static async Task<T> GetJsonAsync<T>(string requestUri) where T : class
        {
            T obj = null;
            using HttpClient client = new HttpClient();
            using HttpResponseMessage content = await client.GetAsync(requestUri);
            try {
                string json = content.Content.ReadAsStringAsync().Result;
                obj = JsonSerializer.Deserialize<T>(json);
            } catch (Exception e) {
                // TODO: Logging.
            } finally {
                content?.Dispose();
                client?.Dispose();
            }

            return obj;
        }

        public static async Task<HttpResponseMessage> PostJsonAsync<T>(string requestUri, T content)
        {
            HttpResponseMessage response = null;
            using HttpClient client = new HttpClient();
            try {
                string jsonString = JsonSerializer.Serialize(content);
                StringContent stringContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                response = await client.PostAsync(requestUri, stringContent);
            } catch (Exception e) {
                // TODO: Logging.
            } finally {
                client.Dispose();
            }

            return response;
        }

        public static async Task<HttpResponseMessage> PutJsonAsync<T>(string requestUri, T content)
        {
            HttpResponseMessage response = null;
            using HttpClient client = new HttpClient();
            try {
                string jsonString = JsonSerializer.Serialize(content);
                StringContent stringContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                response = await client.PutAsync(requestUri, stringContent);
            } catch (Exception e) {
                // TODO: Logging.
            } finally {
                client.Dispose();
            }

            return response;
        }
    }
}
