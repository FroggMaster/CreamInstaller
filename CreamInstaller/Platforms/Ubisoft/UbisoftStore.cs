using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CreamInstaller.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CreamInstaller.Platforms.Ubisoft;

internal static class UbisoftStore
{
    private const string AlgoliaAppId = "XELY3U4LOD";
    private const string AlgoliaApiKey = "5638539fd9edb8f2c6b024b49ec375bd";

    internal static async Task<List<(string id, string name)>> QuerySearch(string keyword)
    {
        List<(string, string)> results = [];
        try
        {
            string url = $"https://{AlgoliaAppId.ToLowerInvariant()}-dsn.algolia.net/1/indexes/*/queries" +
                         "?x-algolia-agent=Algolia%20for%20JavaScript%20(3.35.1)%3B%20Browser" +
                         $"&x-algolia-application-id={AlgoliaAppId}" +
                         $"&x-algolia-api-key={AlgoliaApiKey}";

            var requestBody = new
            {
                requests = new[]
                {
                    new
                    {
                        indexName = "us_product_suggestion",
                        query = keyword,
                        @params = "hitsPerPage=1000"
                    }
                }
            };

            string payloadJson = JsonConvert.SerializeObject(requestBody);
            using HttpContent content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");
            HttpClient client = HttpClientManager.HttpClient;
            if (client is null)
                return results;

            HttpResponseMessage httpResponse = await client.PostAsync(new Uri(url), content);
            _ = httpResponse.EnsureSuccessStatusCode();
            string response = await httpResponse.Content.ReadAsStringAsync();
            JObject root = JObject.Parse(response);
            JToken hits = root["results"]?[0]?["hits"];
            if (hits is null)
                return results;

            // Extract significant query words (2+ chars) for substring filtering
            string[] queryTerms = keyword.Split([' '], StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 2)
                .Select(t => t.ToLowerInvariant())
                .ToArray();

            foreach (JToken hit in hits)
            {
                string title = hit["title"]?.ToString();
                string id = hit["objectID"]?.ToString();
                string platform = hit["Platform"]?.ToString();
                string productType = hit["productTypeSelect"]?.ToString();
                string edition = hit["Edition"]?.ToString() ?? "";
                string game = hit["Game"]?.ToString() ?? "";

                // Skip non-game items (currency, merchandise, etc.)
                bool isGame = string.IsNullOrEmpty(productType) ||
                              string.Equals(productType, "game", StringComparison.OrdinalIgnoreCase);

                // Skip DLCs, extensions, season passes, currency packs
                if (!string.IsNullOrEmpty(edition) &&
                    (edition.Contains("Extension", StringComparison.OrdinalIgnoreCase) ||
                     edition.Contains("Season Pass", StringComparison.OrdinalIgnoreCase) ||
                     edition.Contains("Currency", StringComparison.OrdinalIgnoreCase) ||
                     edition.Contains("Pack", StringComparison.OrdinalIgnoreCase)))
                    continue;

                // Also skip items not available on PC
                bool isPc = string.IsNullOrEmpty(platform) ||
                            platform.Contains("PC", StringComparison.OrdinalIgnoreCase);

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(id)
                    || !isGame || !isPc || results.Any(r => r.Item1 == id))
                    continue;

                // Post-filter: at least one query word must appear as a substring
                // in the title or Game field. This eliminates Algolia's overly
                // aggressive fuzzy/typo matches (e.g. "AssAss" → "Star Wars").
                if (queryTerms.Length > 0)
                {
                    string lowerTitle = title.ToLowerInvariant();
                    string lowerGame = game.ToLowerInvariant();
                    if (!queryTerms.Any(t => lowerTitle.Contains(t) || lowerGame.Contains(t)))
                        continue;
                }

                results.Add((id, title));
            }
        }
        catch
        {
            // ignored — search failure is non-fatal
        }

        return results;
    }
}
