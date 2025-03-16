using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using GoldbergGUI.Core.Models;
using GoldbergGUI.Core.Utils;
using GoldbergGUI.Core.ViewModels;
using MvvmCross.Logging;
using NinjaNye.SearchExtensions;
using SQLite;
using SteamStorefrontAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GoldbergGUI.Core.Services
{
    // gets info from steam api
    public interface ISteamService
    {
        public void Initialize(IMvxLog log, GoldbergGlobalConfiguration globalConfiguration);
        public Task<IEnumerable<SteamApp>> GetListOfAppsByName(string name, string language);
        public Task<SteamApp> GetAppByName(string name);
        public Task<SteamApp> GetAppById(int appid);
        public Task<List<Achievement>> GetListOfAchievements(SteamApp steamApp);
        public Task<List<DlcApp>> GetListOfDlc(SteamApp steamApp, bool useSteamDb);
    }

    public class SteamItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("tiny_image")]
        public string ImageUrl { get; set; }
    }

    public class SteamResponse
    {
        public List<SteamItem> Items { get; set; }
    }

    public class SteamParser
    {
        public static List<SteamItem> ParseGames(string json)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = JsonSerializer.Deserialize<SteamResponse>(json, options);

            var games = new List<SteamItem>();
            if (response?.Items != null)
            {
                foreach (var item in response.Items)
                {
                    games.Add(new SteamItem
                    {
                        Name = item.Name,
                        Id = item.Id,
                        ImageUrl = item.ImageUrl
                    });
                }
            }
            return games;
        }
    }

    public class SteamService : ISteamService
    {
        private IMvxLog _log;
        private string _language;
        private string _useragent;

        public void Initialize(IMvxLog log, GoldbergGlobalConfiguration globalConfiguration)
        {
            _log = log;
            _language = globalConfiguration.Language;
            _useragent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.246";
        }

        public async Task<IEnumerable<SteamItem>> GetListOfAppsByName(string name, string language)
        {
            _log.Info($"Trying to get list of apps by name {name}");
            var comparableName = PrepareStringToCompare(name);
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(_useragent);
            var url = $"https://store.steampowered.com/api/storesearch/?term={name}&l={language}&cc=US";
            var response = await client.GetAsync(url).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var games = SteamParser.ParseGames(responseBody);

            return games;
        }

        public async Task<SteamApp> GetAppById(int appid)
        {
            _log.Info($"Trying to get app with ID {appid}");

            return app;
        }

        public async Task<List<Achievement>> GetListOfAchievements(SteamItem steamApp)
        {
            var achievementList = new List<Achievement>();
            if (steamApp == null)
            {
                return achievementList;
            }

            _log.Info($"Getting achievements for App {steamApp}");

            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(_useragent);

            var apiUrl = $"{GameSchemaUrl}?key={Key()}&appid={steamApp.AppId}&l={_language}";

            var response = await client.GetAsync(apiUrl);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var jsonResponse = JsonDocument.Parse(responseBody);
            if (jsonResponse.RootElement.TryGetProperty("game", out JsonElement gameElement) &&
                gameElement.TryGetProperty("availableGameStats", out JsonElement statsElement) &&
                statsElement.TryGetProperty("achievements", out JsonElement achievementData))
            {
                achievementList = JsonSerializer.Deserialize<List<Achievement>>(achievementData.GetRawText());
            }
            else
            {
                _log.Error("Required properties not found in the JSON response.");
            }
            return achievementList;
        }

        public async Task<List<DlcApp>> GetListOfDlc(SteamApp steamApp, bool useSteamDb)
        {
            var dlcList = new List<DlcApp>();
            if (steamApp != null)
            {
                _log.Info($"Get DLC for App {steamApp}");
                var task = AppDetails.GetAsync(steamApp.AppId);
                var steamAppDetails = await task.ConfigureAwait(true);
                if (steamAppDetails.Type == AppTypeGame)
                {
                    steamAppDetails.DLC.ForEach(async x =>
                    {
                        var result = await _db.Table<SteamApp>().Where(z => z.AppType == AppTypeDlc)
                                         .FirstOrDefaultAsync(y => y.AppId.Equals(x)).ConfigureAwait(true)
                                     ?? new SteamApp() { AppId = x, Name = $"Unknown DLC {x}", ComparableName = $"unknownDlc{x}", AppType = AppTypeDlc };
                        dlcList.Add(new DlcApp(result));
                        _log.Debug($"{result.AppId}={result.Name}");
                    });

                    _log.Info("Got DLC successfully...");

                    // Get DLC from SteamDB
                    // Get Cloudflare cookie (not implemented)
                    // Scrape and parse HTML page
                    // Add missing to DLC list

                    // Return current list if we don't intend to use SteamDB
                    if (!useSteamDb) return dlcList;

                    try
                    {
                        var steamDbUri = new Uri($"https://steamdb.info/app/{steamApp.AppId}/dlc/");

                        var client = new HttpClient();
                        client.DefaultRequestHeaders.UserAgent.ParseAdd(_useragent);

                        _log.Info($"Get SteamDB App {steamApp}");
                        var httpCall = client.GetAsync(steamDbUri);
                        var response = await httpCall.ConfigureAwait(false);
                        _log.Debug(httpCall.Status.ToString());
                        _log.Debug(response.EnsureSuccessStatusCode().ToString());

                        var readAsStringAsync = response.Content.ReadAsStringAsync();
                        var responseBody = await readAsStringAsync.ConfigureAwait(false);
                        _log.Debug(readAsStringAsync.Status.ToString());

                        var parser = new HtmlParser();
                        var doc = parser.ParseDocument(responseBody);

                        var query1 = doc.QuerySelector("#dlc");
                        if (query1 != null)
                        {
                            _log.Info("Got list of DLC from SteamDB.");
                            var query2 = query1.QuerySelectorAll(".app");
                            foreach (var element in query2)
                            {
                                var dlcId = element.GetAttribute("data-appid");
                                var query3 = element.QuerySelectorAll("td");
                                var dlcName = query3 != null
                                    ? query3[1].Text().Replace("\n", "").Trim()
                                    : $"Unknown DLC {dlcId}";
                                var dlcApp = new DlcApp { AppId = Convert.ToInt32(dlcId), Name = dlcName };
                                var i = dlcList.FindIndex(x => x.AppId.Equals(dlcApp.AppId));
                                if (i > -1)
                                {
                                    if (dlcList[i].Name.Contains("Unknown DLC")) dlcList[i] = dlcApp;
                                }
                                else
                                {
                                    dlcList.Add(dlcApp);
                                }
                            }

                            dlcList.ForEach(x => _log.Debug($"{x.AppId}={x.Name}"));
                            _log.Info("Got DLC from SteamDB successfully...");
                        }
                        else
                        {
                            _log.Error("Could not get DLC from SteamDB!");
                        }
                    }
                    catch (Exception e)
                    {
                        _log.Error("Could not get DLC from SteamDB! Skipping...");
                        _log.Error(e.ToString);
                    }
                }
                else
                {
                    _log.Error("Could not get DLC: Steam App is not of type \"game\"");
                }
            }
            else
            {
                _log.Error("Could not get DLC: Invalid Steam App");
            }

            return dlcList;
        }

        private static string PrepareStringToCompare(string name)
        {
            return Regex.Replace(name, Misc.AlphaNumOnlyRegex, "").ToLower();
        }
    }
}