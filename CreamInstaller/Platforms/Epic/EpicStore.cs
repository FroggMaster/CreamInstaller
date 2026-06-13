using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using CreamInstaller.Platforms.Epic.GraphQL;
using CreamInstaller.Utility;
using Newtonsoft.Json;



namespace CreamInstaller.Platforms.Epic;

internal static class EpicStore
{
    private const int Cooldown = 600;

    internal static async Task<List<(string id, string name, string product, string icon, string developer)>>
        QueryCatalog(string categoryNamespace)
    {
        List<(string id, string name, string product, string icon, string developer)> dlcIds = [];
        string cacheFile = ProgramData.AppInfoPath + @$"\{categoryNamespace}.json";
        string fileContent = cacheFile.ReadFile();
        if (string.IsNullOrWhiteSpace(fileContent) || fileContent.Trim() == "null")
        {
            cacheFile.DeleteFile();
        }
        bool cachedExists = cacheFile.FileExists();
        Response response = null;
        if (!cachedExists || ProgramData.CheckCooldown(categoryNamespace, Cooldown))
        {
            response = await QueryGraphQL(categoryNamespace);
            if (response is null)
            {
                ProgramData.LogWarning("Epic QueryGraphQL returned null for " + categoryNamespace);
            }
            try
            {
                cacheFile.WriteFile(JsonConvert.SerializeObject(response, Formatting.Indented));
            }
            catch
            {
                // ignored
            }
        }
        else
            try
            {
                response = JsonConvert.DeserializeObject<Response>(cacheFile.ReadFile());
            }
            catch
            {
                cacheFile.DeleteFile();
            }

        if (response is null || response.Data?.Catalog is null)
            return dlcIds;
        List<Element> searchStore = [..response.Data.Catalog.SearchStore?.Elements ?? []];
        foreach (Element element in searchStore)
        {
            string title = element.Title;
            string product = element.CatalogNs?.Mappings is { Length: > 0 }
                ? element.CatalogNs.Mappings.First().PageSlug
                : null;
            string icon = null;
            for (int i = 0; i < element.KeyImages?.Length; i++)
            {
                KeyImage keyImage = element.KeyImages[i];
                if (keyImage.Type != "DieselStoreFront")
                    continue;
                icon = keyImage.Url.ToString();
                break;
            }

            foreach (Item item in element.Items)
                dlcIds.Populate(item.Id, title, product, icon, null, element.Items.Length == 1);
        }

        List<Element> catalogOffers = [..response.Data.Catalog.CatalogOffers?.Elements ?? []];
        foreach (Element element in catalogOffers)
        {
            string title = element.Title;
            string product = element.CatalogNs?.Mappings is { Length: > 0 }
                ? element.CatalogNs.Mappings.First().PageSlug
                : null;
            string icon = null;
            for (int i = 0; i < element.KeyImages?.Length; i++)
            {
                KeyImage keyImage = element.KeyImages[i];
                if (keyImage.Type != "Thumbnail")
                    continue;
                icon = keyImage.Url.ToString();
                break;
            }

            foreach (Item item in element.Items)
                dlcIds.Populate(item.Id, title, product, icon, item.Developer, element.Items.Length == 1);
        }

        return dlcIds;
    }

    private static void Populate(
        this List<(string id, string name, string product, string icon, string developer)> dlcIds, string id,
        string title,
        string product, string icon, string developer, bool canOverwrite = false)
    {
        if (id == null)
            return;
        bool found = false;
        for (int i = 0; i < dlcIds.Count; i++)
        {
            (string id, string name, string product, string icon, string developer) app = dlcIds[i];
            if (app.id != id)
                continue;

            found = true;
            dlcIds[i] = canOverwrite
                ? (app.id, title ?? app.name, product ?? app.product, icon ?? app.icon, developer ?? app.developer)
                : (app.id, app.name ?? title, app.product ?? product, app.icon ?? icon, app.developer ?? developer);
            break;
        }

        if (!found)
            dlcIds.Add((id, title, product, icon, developer));
    }

    public static bool EpicBool = true;

    internal static async Task<List<(string @namespace, string name)>> QuerySearch(string keyword)
    {
        List<(string, string)> results = [];
        try
        {
            string query = """
                query searchByKeyword($keywords: String!) {
                    Catalog {
                        searchStore(keywords: $keywords, category: "games/edition/base", count: 10, country: "US", locale: "en-US", allowCountries: "US") {
                            elements {
                                title
                                namespace
                            }
                        }
                    }
                }
                """;
            var payload = new { query, variables = new { keywords = keyword } };
            string payloadJson = JsonConvert.SerializeObject(payload);
            using HttpContent content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");
            HttpClient client = HttpClientManager.HttpClient;
            if (client is null)
                return results;
            HttpResponseMessage httpResponse =
                await client.PostAsync(new Uri("https://launcher.store.epicgames.com/graphql"), content);
            _ = httpResponse.EnsureSuccessStatusCode();
            string response = await httpResponse.Content.ReadAsStringAsync();
            Newtonsoft.Json.Linq.JObject root = Newtonsoft.Json.Linq.JObject.Parse(response);
            Newtonsoft.Json.Linq.JToken elements = root["data"]?["Catalog"]?["searchStore"]?["elements"];
            if (elements is null)
                return results;
            foreach (Newtonsoft.Json.Linq.JToken el in elements)
            {
                string name = el["title"]?.ToString();
                string ns = el["namespace"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(ns)
                    && results.All(r => r.Item1 != ns))
                    results.Add((ns, name));
            }
        }
        catch
        {
            // ignored
        }
        return results;
    }

    private static async Task<Response> QueryGraphQL(string categoryNamespace)
    {
        try
        {
            string encoded = HttpUtility.UrlEncode(categoryNamespace);
            Request request = new(encoded);
            string payload = JsonConvert.SerializeObject(request);
            using HttpContent content = new StringContent(payload);
            content.Headers.ContentType = new("application/json");
            HttpClient client = HttpClientManager.HttpClient;
            if (client is null)
            {
                ProgramData.LogWarning("Epic GraphQL client returned null");
                return null;
            }
            HttpResponseMessage httpResponse =
                await client.PostAsync(new Uri("https://launcher.store.epicgames.com/graphql"), content);
            _ = httpResponse.EnsureSuccessStatusCode();
            string response = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Response>(response);
        }
        catch
        {
            return null;
        }
    }
}