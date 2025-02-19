using GoldbergGUI.Core.Models;
using GoldbergGUI.Core.Utils;
using MvvmCross.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Configuration.Internal;
using System.Diagnostics;

namespace GoldbergGUI.Core.Services
{
    // downloads and updates goldberg emu
    // sets up config files
    // does file copy stuff
    public interface IGoldbergService
    {
        public Task<GoldbergGlobalConfiguration> Initialize(IMvxLog log);
        public Task<GoldbergConfiguration> Read(string path);
        public Task Save(string path, GoldbergGlobalConfiguration globalconfig, GoldbergConfiguration configuration);
        public Task<GoldbergGlobalConfiguration> GetGlobalSettings();
        public Task SetGlobalSettings(GoldbergGlobalConfiguration configuration);
        public bool GoldbergApplied(string path);
        public Task GenerateInterfacesFile(string filePath);
        public List<string> Languages();
    }

    // ReSharper disable once UnusedType.Global
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class GoldbergService : IGoldbergService
    {
        private IMvxLog _log;
        private const string DefaultAccountName = "Mr_Goldberg";
        private const long DefaultSteamId = 76561197960287930;
        private const string DefaultLanguage = "english";
        private const string GoldbergUrl = "https://github.com/Detanup01/gbe_fork/releases/latest";
        private readonly string _goldbergZipPath = Path.Combine(Directory.GetCurrentDirectory(), "goldberg.7z");
        private readonly string _goldbergPath = Path.Combine(Directory.GetCurrentDirectory(), "goldberg");

        private static readonly string GlobalSettingsPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Goldberg SteamEmu Saves");

        private readonly string _accountNamePath = Path.Combine(GlobalSettingsPath, "settings/account_name.txt");
        private readonly string _userSteamIdPath = Path.Combine(GlobalSettingsPath, "settings/user_steam_id.txt");
        private readonly string _languagePath = Path.Combine(GlobalSettingsPath, "settings/language.txt");
        private readonly string _experimentalPath = Path.Combine(GlobalSettingsPath, "settings/experimental.txt");

        private readonly string _customBroadcastIpsPath =
            Path.Combine(GlobalSettingsPath, "settings/custom_broadcasts.txt");

        // ReSharper disable StringLiteralTypo
        private readonly List<string> _interfaceNames =
        [
            "SteamClient",
            "SteamGameServer",
            "SteamGameServerStats",
            "SteamUser",
            "SteamFriends",
            "SteamUtils",
            "SteamMatchMaking",
            "SteamMatchMakingServers",
            "STEAMUSERSTATS_INTERFACE_VERSION",
            "STEAMAPPS_INTERFACE_VERSION",
            "SteamNetworking",
            "STEAMREMOTESTORAGE_INTERFACE_VERSION",
            "STEAMSCREENSHOTS_INTERFACE_VERSION",
            "STEAMHTTP_INTERFACE_VERSION",
            "STEAMUNIFIEDMESSAGES_INTERFACE_VERSION",
            "STEAMUGC_INTERFACE_VERSION",
            "STEAMAPPLIST_INTERFACE_VERSION",
            "STEAMMUSIC_INTERFACE_VERSION",
            "STEAMMUSICREMOTE_INTERFACE_VERSION",
            "STEAMHTMLSURFACE_INTERFACE_VERSION_",
            "STEAMINVENTORY_INTERFACE_V",
            "SteamController",
            "SteamMasterServerUpdater",
            "STEAMVIDEO_INTERFACE_V"
        ];

        // Call Download
        // Get global settings
        public async Task<GoldbergGlobalConfiguration> Initialize(IMvxLog log)
        {
            _log = log;

            var download = await Download().ConfigureAwait(false);
            if (download)
            {
                await Extract(_goldbergZipPath).ConfigureAwait(false);
            }

            return await GetGlobalSettings().ConfigureAwait(false);
        }

        public async Task<GoldbergGlobalConfiguration> GetGlobalSettings()
        {
            _log.Info("Getting global settings...");
            var accountName = DefaultAccountName;
            var steamId = DefaultSteamId;
            var language = DefaultLanguage;
            var experimental = false;
            var customBroadcastIps = new List<string>();
            if (!File.Exists(GlobalSettingsPath)) Directory.CreateDirectory(Path.Join(GlobalSettingsPath, "settings"));
            await Task.Run(() =>
            {
                if (File.Exists(_accountNamePath)) accountName = File.ReadLines(_accountNamePath).First().Trim();
                if (File.Exists(_userSteamIdPath) &&
                    !long.TryParse(File.ReadLines(_userSteamIdPath).First().Trim(), out steamId) &&
                    steamId < 76561197960265729 && steamId > 76561202255233023)
                {
                    _log.Error("Invalid User Steam ID! Using default Steam ID...");
                    steamId = DefaultSteamId;
                }

                if (File.Exists(_languagePath)) language = File.ReadLines(_languagePath).First().Trim();
                if (File.Exists(_experimentalPath)) experimental = File.ReadLines(_experimentalPath).First().Trim() == "true";
                if (File.Exists(_customBroadcastIpsPath))
                    customBroadcastIps.AddRange(
                        File.ReadLines(_customBroadcastIpsPath).Select(line => line.Trim()));
            }).ConfigureAwait(false);
            _log.Info("Got global settings.");
            return new GoldbergGlobalConfiguration
            {
                AccountName = accountName,
                UserSteamId = steamId,
                Language = language,
                Experimental = experimental,
                CustomBroadcastIps = customBroadcastIps
            };
        }

        public async Task SetGlobalSettings(GoldbergGlobalConfiguration c)
        {
            var accountName = c.AccountName;
            var userSteamId = c.UserSteamId;
            var language = c.Language;
            var experimental = c.Experimental;
            var customBroadcastIps = c.CustomBroadcastIps;
            _log.Info("Setting global settings...");
            // Account Name
            if (!string.IsNullOrEmpty(accountName))
            {
                _log.Info("Setting account name...");
                if (!File.Exists(_accountNamePath))
                    await File.Create(_accountNamePath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_accountNamePath, accountName).ConfigureAwait(false);
            }
            else
            {
                _log.Info("Invalid account name! Skipping...");
                if (!File.Exists(_accountNamePath))
                    await File.Create(_accountNamePath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_accountNamePath, DefaultAccountName).ConfigureAwait(false);
            }

            // User SteamID
            if (userSteamId >= 76561197960265729 && userSteamId <= 76561202255233023)
            {
               _log.Info("Setting user Steam ID...");
                if (!File.Exists(_userSteamIdPath))
                    await File.Create(_userSteamIdPath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_userSteamIdPath, userSteamId.ToString()).ConfigureAwait(false);
            }
            else
            {
                _log.Info("Invalid user Steam ID! Skipping...");
                if (!File.Exists(_userSteamIdPath))
                    await File.Create(_userSteamIdPath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_userSteamIdPath, DefaultSteamId.ToString()).ConfigureAwait(false);
            }

            // Language
            if (!string.IsNullOrEmpty(language))
            {
                _log.Info("Setting language...");
                if (!File.Exists(_languagePath))
                    await File.Create(_languagePath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_languagePath, language).ConfigureAwait(false);
            }
            else
            {
                _log.Info("Invalid language! Skipping...");
                if (!File.Exists(_languagePath))
                    await File.Create(_languagePath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_languagePath, DefaultLanguage).ConfigureAwait(false);
            }

            // Experimental
            if (!experimental)
            {
                _log.Info("Disabling experimental client...");
                if (!File.Exists(_experimentalPath))
                    await File.Create(_experimentalPath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_experimentalPath, "false").ConfigureAwait(false);
            }
            else
            {
                _log.Info("Setting experimental client...");
                if (!File.Exists(_experimentalPath))
                    await File.Create(_experimentalPath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_experimentalPath, "true").ConfigureAwait(false);
            }

            // Custom Broadcast IPs
            if (customBroadcastIps != null && customBroadcastIps.Count > 0)
            {
                _log.Info("Setting custom broadcast IPs...");
                var result =
                    customBroadcastIps.Aggregate("", (current, address) => $"{current}{address}\n");
                if (!File.Exists(_customBroadcastIpsPath))
                    await File.Create(_customBroadcastIpsPath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_customBroadcastIpsPath, result).ConfigureAwait(false);
            }
            else
            {
                _log.Info("Empty list of custom broadcast IPs! Skipping...");
                await Task.Run(() => File.Delete(_customBroadcastIpsPath)).ConfigureAwait(false);
            }
            //_log.Info("Setting global configuration finished.");
        }

        // If first time, call GenerateInterfaces
        // else try to read config
        public async Task<GoldbergConfiguration> Read(string path)
        {
            _log.Info("Reading configuration...");
            var experimentalNow = false;
            var appId = -1;
            var achievementList = new List<Achievement>();
            var dlcList = new List<DlcApp>();
            var steamAppidTxt = Path.Combine(path, "steam_appid.txt");
            if (File.Exists(steamAppidTxt))
            {
                _log.Info("Getting AppID...");
                await Task.Run(() => int.TryParse(File.ReadLines(steamAppidTxt).First().Trim(), out appId))
                    .ConfigureAwait(false);
            }
            else
            {
                _log.Info(@"""steam_appid.txt"" missing! Skipping...");
            }

            var achievementJson = Path.Combine(path, "steam_settings", "achievements.json");
            if (File.Exists(achievementJson))
            {
                _log.Info("Getting achievements...");
                var json = await File.ReadAllTextAsync(achievementJson)
                    .ConfigureAwait(false);
                achievementList = System.Text.Json.JsonSerializer.Deserialize<List<Achievement>>(json);
            }
            else
            {
                _log.Info(@"""steam_settings/achievements.json"" missing! Skipping...");
            }

            if (File.Exists(_experimentalPath))
            {
                _log.Info("Getting experimental settings...");
                experimentalNow = File.ReadLines(_experimentalPath).First().Trim() == "true";
            }
            else
            {
                _log.Info(@"""steam_settings/experimental.txt"" missing! Skipping...");
            }

            var dlcTxt = Path.Combine(path, "steam_settings", "DLC.txt");
            var appPathTxt = Path.Combine(path, "steam_settings", "app_paths.txt");
            if (File.Exists(dlcTxt))
            {
                _log.Info("Getting DLCs...");
                var readAllLinesAsync = await File.ReadAllLinesAsync(dlcTxt).ConfigureAwait(false);
                var expression = MyRegex();
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var line in readAllLinesAsync)
                {
                    var match = expression.Match(line);
                    if (match.Success)
                        dlcList.Add(new DlcApp()
                        {
                            AppId = Convert.ToInt32(match.Groups["id"].Value),
                            Name = match.Groups["name"].Value
                        });
                }

                // ReSharper disable once InvertIf
                if (File.Exists(appPathTxt))
                {
                    var appPathAllLinesAsync = await File.ReadAllLinesAsync(appPathTxt).ConfigureAwait(false);
                    var appPathExpression = new Regex(@"(?<id>.*) *= *(?<appPath>.*)");
                    foreach (var line in appPathAllLinesAsync)
                    {
                        var match = appPathExpression.Match(line);
                        if (!match.Success) continue;
                        var i = dlcList.FindIndex(x =>
                            x.AppId.Equals(Convert.ToInt32(match.Groups["id"].Value)));
                        dlcList[i].AppPath = match.Groups["appPath"].Value;
                    }
                }
            }
            else
            {
                _log.Info(@"""steam_settings/DLC.txt"" missing! Skipping...");
            }

            return new GoldbergConfiguration
            {
                AppId = appId,
                Achievements = achievementList,
                DlcList = dlcList,
                ExperimentalNow = experimentalNow,
                Offline = File.Exists(Path.Combine(path, "steam_settings", "offline.txt")),
                DisableNetworking = File.Exists(Path.Combine(path, "steam_settings", "disable_networking.txt")),
                DisableOverlay = File.Exists(Path.Combine(path, "steam_settings", "disable_overlay.txt"))
            };
        }

        // If first time, rename original SteamAPI DLL to steam_api(64)_o.dll
        // If not, rename current SteamAPI DLL to steam_api(64).dll.backup
        // Copy Goldberg DLL to path
        // Save configuration files
        public async Task Save(string path, GoldbergGlobalConfiguration g, GoldbergConfiguration c)
        {
            // Verify extra tools are available
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), "tools");
            if (!Directory.Exists(toolsPath))
            {
                Directory.CreateDirectory("tools");
            }
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "tools", "generate_emu_config", "generate_emu_config.exe")))
            {
                var toolsArchivePath = Path.Combine(toolsPath, "generate_emu_config-win.7z");
                if (!File.Exists(toolsArchivePath))
                {
                    File.Copy("Configs/generate_emu_config-win.7z", toolsArchivePath, true);
                }
                using (var archive = await Task.Run(() => SevenZipArchive.Open(toolsArchivePath)).ConfigureAwait(false))
                {
                    archive.ExtractToDirectory(toolsPath);
                }
            }

            // Save game settings
            _log.Info("Saving configuration...");
            // DLL setup
            _log.Info("Running DLL setup...");
            const string x86Name = "steam_api";
            const string x64Name = "steam_api64";

            // Experimental or Regular DLL
            if (File.Exists(Path.Combine(path, $"{x86Name}.dll")))
            {
                CopyDllFiles(path, x86Name, c.ExperimentalNow);
            }

            if (File.Exists(Path.Combine(path, $"{x64Name}.dll")))
            {
                CopyDllFiles(path, x64Name, c.ExperimentalNow);
            }
            _log.Info("DLL setup finished!");

            // Create steam_settings folder if missing
            _log.Info("Saving settings...");
            if (!Directory.Exists(Path.Combine(path, "steam_settings")))
            {
                Directory.CreateDirectory(Path.Combine(path, "steam_settings"));
            }

            // create steam_appid.txt
            await File.WriteAllTextAsync(Path.Combine(path, "steam_appid.txt"), c.AppId.ToString())
                .ConfigureAwait(false);

            // Create main, overlay, and user config files
            // Main
            _log.Info("Setting up configs.main.ini");
            var mainConfig = $"[main::general]\nnew_app_ticket = 1\ngc_token = 1\n[main::connectivity]\ndisable_networking = {(c.DisableNetworking ? 1 : 0)}\noffline = {(c.Offline ? 1 : 0)}";
            await File.WriteAllTextAsync(Path.Combine(path, "steam_settings", "configs.main.ini"), mainConfig)
                .ConfigureAwait(false);

            // Overlay
            _log.Info("Settings up configs.overlay.ini");
            var overlayConfig = $"[overlay::general]\nenable_experimental_overlay={(c.DisableOverlay ? 0 : 1)}\n";
            overlayConfig += "hook_delay_sec=5\nrenderer_detector_timeout_sec=35\ndisable_achievement_progress=0\n";
            var overlayConfig2 = File.ReadAllText("Configs/configs.overlay.ini.template");
            overlayConfig += overlayConfig2;
            await File.WriteAllTextAsync(Path.Combine(path, "steam_settings", "configs.overlay.ini"), overlayConfig)
                .ConfigureAwait(false);

            // User
            _log.Info("Setting up configs.user.ini");
            var userConfig = $"[user::general]\naccount_name={g.AccountName}\naccount_steamid={g.UserSteamId}\nlanguage={g.Language}";
            await File.WriteAllTextAsync(Path.Combine(path, "steam_settings", "configs.user.ini"), userConfig)
                .ConfigureAwait(false);

            // Copy other files
            _log.Info("Copying other files...");
            // Font
            if (!Directory.Exists(Path.Combine(path, "steam_settings", "fonts")))
            {
                Directory.CreateDirectory(Path.Combine(path, "steam_settings", "fonts"));
            }
            File.Copy("Media/fonts/Roboto-Medium.ttf", Path.Combine(path, "steam_settings", "fonts", "Roboto-Medium.ttf"), true);

            // Generate controller config
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = Path.Combine(Directory.GetCurrentDirectory(), "tools", "generate_emu_config", "generate_emu_config.exe");
                    p.StartInfo.Arguments = $"{c.AppId} -anon -skip_ach -skip_inv";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "tools", "generate_emu_config");
                    p.StartInfo.Verb = "runas";
                    p.Start();
                    p.WaitForExit();
                    int code = p.ExitCode;
                }
            }
            catch (Exception e)
            {
                _log.Error(e.ToString());
            }

            // Copy controller config
            if (!Directory.Exists(Path.Combine(path, "steam_settings", "controller")))
            {
                Directory.CreateDirectory(Path.Combine(path, "steam_settings", "controller"));
            }
            var newcontroller = Path.Combine(Directory.GetCurrentDirectory(), "tools", "generate_emu_config", "output", c.AppId.ToString(), "steam_settings", "controller");
            if (Directory.Exists(newcontroller)){
                Copy(newcontroller, Path.Combine(path, "steam_settings", "controller"));
                if (!Directory.Exists(Path.Combine(path, "steam_settings", "controller", "glyphs")))
                {
                    Directory.CreateDirectory(Path.Combine(path, "steam_settings", "controller", "glyphs"));
                }
                Copy("Media/glyphs", Path.Combine(path, "steam_settings", "controller", "glyphs"));
            }


            // Achievements + Images
            if (c.Achievements.Count > 0)
            {
                _log.Info("Downloading images...");
                var imagePath = Path.Combine(path, "steam_settings", "images");
                Directory.CreateDirectory(imagePath);

                foreach (var achievement in c.Achievements)
                {
                    await DownloadImageAsync(imagePath, achievement.Icon);
                    await DownloadImageAsync(imagePath, achievement.IconGray);

                    // Update achievement list to point to local images instead
                    achievement.Icon = $"images/{Path.GetFileName(achievement.Icon)}";
                    achievement.IconGray = $"images/{Path.GetFileName(achievement.IconGray)}";
                }

                _log.Info("Saving achievements...");

                var achievementJson = System.Text.Json.JsonSerializer.Serialize(
                    c.Achievements,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        WriteIndented = true
                    });
                await File.WriteAllTextAsync(Path.Combine(path, "steam_settings", "achievements.json"), achievementJson)
                    .ConfigureAwait(false);

                _log.Info("Finished saving achievements.");
            }
            else
            {
                _log.Info("No achievements set! Removing achievement files...");
                var imagePath = Path.Combine(path, "steam_settings", "images");
                if (Directory.Exists(imagePath))
                {
                    Directory.Delete(imagePath);
                }
                var achievementPath = Path.Combine(path, "steam_settings", "achievements");
                if (File.Exists(achievementPath))
                {
                    File.Delete(achievementPath);
                }
                _log.Info("Removed achievement files.");
            }

            // DLC + App path
            if (c.DlcList.Count > 0)
            {
                _log.Info("Saving DLC settings...");
                var dlcContent = "";
                //var depotContent = "";
                var appPathContent = "";
                c.DlcList.ForEach(x =>
                {
                    dlcContent += $"{x}\n";
                    //depotContent += $"{x.DepotId}\n";
                    if (!string.IsNullOrEmpty(x.AppPath))
                        appPathContent += $"{x.AppId}={x.AppPath}\n";
                });
                dlcContent = $"[app::dlcs]\nunlock_all=0\n{dlcContent.TrimEnd('\n')}";
                await File.WriteAllTextAsync(Path.Combine(path, "steam_settings", "configs.app.ini"), dlcContent)
                    .ConfigureAwait(false);

                /*if (!string.IsNullOrEmpty(depotContent))
                {
                    await File.WriteAllTextAsync(Path.Combine(path, "steam_settings", "depots.txt"), depotContent)
                        .ConfigureAwait(false);
                }*/


                if (!string.IsNullOrEmpty(appPathContent))
                {
                    await File.WriteAllTextAsync(Path.Combine(path, "steam_settings", "app_paths.txt"), appPathContent)
                        .ConfigureAwait(false);
                }
                else
                {
                    if (File.Exists(Path.Combine(path, "steam_settings", "app_paths.txt")))
                        File.Delete(Path.Combine(path, "steam_settings", "app_paths.txt"));
                }
                _log.Info("Saved DLC settings.");

            }
            else
            {
                _log.Info("No DLC set! Removing DLC configuration files...");
                if (File.Exists(Path.Combine(path, "steam_settings", "configs.app.ini")))
                    File.Delete(Path.Combine(path, "steam_settings", "configs.app.ini"));
                if (File.Exists(Path.Combine(path, "steam_settings", "app_paths.txt")))
                    File.Delete(Path.Combine(path, "steam_settings", "app_paths.txt"));
                _log.Info("Removed DLC configuration files.");
            }
        }

        private void CopyDllFiles(string path, string name, bool exp)
        {
            var steamApiDll = Path.Combine(path, $"{name}.dll");
            var originalDll = Path.Combine(path, $"{name}_o.dll");
            var guiBackup = Path.Combine(path, $".{name}.dll.GOLDBERGGUIBACKUP");
            var goldbergDll = Path.Combine(_goldbergPath, $"{name}.dll");

            if (exp)
            {
                goldbergDll = Path.Combine(_goldbergPath, $"{name}_exp.dll");
            }

            if (!File.Exists(originalDll))
            {
                _log.Info("Back up original Steam API DLL...");
                File.Move(steamApiDll, originalDll);
            }
            else
            {
                File.Move(steamApiDll, guiBackup, true);
                File.SetAttributes(guiBackup, FileAttributes.Hidden);
            }

            _log.Info("Copy Goldberg DLL to target path...");
            File.Copy(goldbergDll, steamApiDll);
        }

        public bool GoldbergApplied(string path)
        {
            var steamSettingsDirExists = Directory.Exists(Path.Combine(path, "steam_settings"));
            var steamAppIdTxtExists = File.Exists(Path.Combine(path, "steam_appid.txt"));
            _log.Debug($"Goldberg applied? {(steamSettingsDirExists && steamAppIdTxtExists).ToString()}");
            return steamSettingsDirExists && steamAppIdTxtExists;
        }

        private async Task<bool> Download()
        {
            // Get webpage
            // Get job id, compare with local if exists, save it if false or missing
            // Get latest archive if mismatch, call Extract
            _log.Info("Initializing download...");
            if (!Directory.Exists(_goldbergPath)) Directory.CreateDirectory(_goldbergPath);
            var client = new HttpClient();
            var response = await client.GetAsync(GoldbergUrl).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var regex = new Regex(
                @"\/Detanup01\/gbe_fork\/releases\/tag\/(?<jobid>release-\d{4}_\d{2}_\d{2})");
            var jobIdPath = Path.Combine(Directory.GetCurrentDirectory(), "job_id");
            var match = regex.Match(body);
            var jobIdRemote = match.Groups["jobid"].Value;
            if (File.Exists(jobIdPath))
            {
                try
                {
                    _log.Info("Check if update is needed...");
                    var jobIdLocal = File.ReadLines(jobIdPath).First().Trim();
                    _log.Debug($"job_id: local {jobIdLocal}; remote {jobIdRemote}");
                    if (jobIdLocal.Equals(jobIdRemote))
                    {
                        _log.Info("Latest Goldberg emulator is already available! Skipping...");
                        return false;
                    }
                    else
                    {
                        _log.Info("New Goldberg emulator is available! Downloading...");
                    }
                }
                catch (Exception)
                {
                    _log.Error("An error occured, local Goldberg setup might be broken!");
                }
            }

            _log.Info("Starting download...");
            string downloadurl = "https://github.com" + match.Value;
            downloadurl = downloadurl.Replace("tag", "download") + "/emu-win-release.7z";
            await StartDownload(downloadurl).ConfigureAwait(false);
            await File.WriteAllTextAsync(jobIdPath, jobIdRemote).ConfigureAwait(false);
            return true;
        }

        private async Task StartDownload(string downloadUrl)
        {
            try
            {
                var client = new HttpClient();
                _log.Debug(downloadUrl);
                await using var fileStream = File.OpenWrite(_goldbergZipPath);
                //client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead)
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Head, downloadUrl);
                var headResponse = await client.SendAsync(httpRequestMessage).ConfigureAwait(false);
                var contentLength = headResponse.Content.Headers.ContentLength;
                await client.GetFileAsync(downloadUrl, fileStream).ContinueWith(async t =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    await fileStream.DisposeAsync().ConfigureAwait(false);
                    var fileLength = new FileInfo(_goldbergZipPath).Length;
                    // Environment.Exit(128);
                    if (contentLength == fileLength)
                    {
                        _log.Info("Download finished!");
                    }
                    else
                    {
                        throw new Exception("File size does not match!");
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ShowErrorMessage();
                _log.Error(e.ToString);
                Environment.Exit(1);
            }
        }

        // Empty subfolder ./goldberg/
        // Extract all from archive to subfolder ./goldberg/
        private async Task Extract(string archivePath)
        {
            var errorOccured = false;
            _log.Debug("Start extraction...");
            string _gb64Path = Path.Combine(_goldbergPath, "release", "regular", "x64", "steam_api64.dll");
            string _gb32Path = Path.Combine(_goldbergPath, "release", "regular", "x32", "steam_api.dll");
            string _gb64Path_Exp = Path.Combine(_goldbergPath, "release", "experimental", "x64", "steam_api64.dll");
            string _gb32Path_Exp = Path.Combine(_goldbergPath, "release", "experimental", "x32", "steam_api.dll");

            // Remove old Goldberg folder
            if (Directory.Exists(_goldbergPath))
            {
                Directory.Delete(_goldbergPath, true);
            }
            // Create new Goldberg folder
            Directory.CreateDirectory(_goldbergPath);

            //try
            //{
            //    _log.Debug("Extracting archive...");
            //    await Task.Run(() =>
            //    {
            //        using var archive = SevenZipArchive.Open(archivePath);
            //        foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
            //        {
            //            entry.WriteToDirectory(_goldbergPath, new ExtractionOptions
            //            {
            //                ExtractFullPath = true,
            //                Overwrite = true
            //            });
            //        }

            //        File.Copy(_gb64Path, Path.Combine(_goldbergPath, "steam_api64.dll"));
            //        File.Copy(_gb32Path, Path.Combine(_goldbergPath, "steam_api.dll"));
            //        File.Copy(_gb64Path_Exp, Path.Combine(_goldbergPath, "steam_api64_exp.dll"));
            //        File.Copy(_gb32Path_Exp, Path.Combine(_goldbergPath, "steam_api_exp.dll"));
            //    }).ConfigureAwait(false);
            //}
            //catch (Exception e)
            //{
            //    errorOccured = true;
            //    _log.Error("Error occurred while extracting 7z archive.");
            //    _log.Error(e.ToString());
            //}

            using (var archive = await Task.Run(() => SevenZipArchive.Open(archivePath)).ConfigureAwait(false))
            {
                foreach (var entry in archive.Entries)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            entry.WriteToDirectory(_goldbergPath, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                        catch (Exception e)
                        {
                            errorOccured = true;
                            _log.Error($"Error while trying to extract {entry.Key}");
                            _log.Error(e.ToString);
                        }
                    }).ConfigureAwait(false);
                }
                try
                {
                    File.Copy(_gb64Path, Path.Combine(_goldbergPath, "steam_api64.dll"));
                    File.Copy(_gb32Path, Path.Combine(_goldbergPath, "steam_api.dll"));
                    File.Copy(_gb64Path_Exp, Path.Combine(_goldbergPath, "steam_api64_exp.dll"));
                    File.Copy(_gb32Path_Exp, Path.Combine(_goldbergPath, "steam_api_exp.dll"));
                }
                catch (Exception e)
                {
                    errorOccured = true;
                    _log.Error("Error occurred while copying Goldberg DLLs.");
                    _log.Error(e.ToString);
                }
            }
            if (errorOccured)
            {
                ShowErrorMessage();
                _log.Warn("Error occured while extracting! Please setup Goldberg manually");
            }
            else
            {
                _log.Info("Extraction was successful!");
            }
        }

        private void ShowErrorMessage()
        {
            if (Directory.Exists(_goldbergPath))
            {
                Directory.Delete(_goldbergPath, true);
            }

            Directory.CreateDirectory(_goldbergPath);
            MessageBox.Show("Could not setup Goldberg Emulator!\n" +
                            "Please download it manually and extract its content into the \"goldberg\" subfolder!");
        }

        // (maybe) check DLL date first
        public async Task GenerateInterfacesFile(string filePath)
        {
            _log.Debug($"GenerateInterfacesFile {filePath}");
            //throw new NotImplementedException();
            // Get DLL content
            var result = new HashSet<string>();
            var dllContent = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            // find interfaces
            foreach (var name in _interfaceNames)
            {
                FindInterfaces(ref result, dllContent, new Regex($"{name}\\d{{3}}"));
                if (!FindInterfaces(ref result, dllContent, new Regex(@"STEAMCONTROLLER_INTERFACE_VERSION\d{3}")))
                {
                    FindInterfaces(ref result, dllContent, new Regex("STEAMCONTROLLER_INTERFACE_VERSION"));
                }
            }

            var dirPath = Path.GetDirectoryName(filePath);
            if (dirPath == null) return;
            await using var destination = File.CreateText(dirPath + "/steam_interfaces.txt");
            foreach (var s in result)
            {
                await destination.WriteLineAsync(s).ConfigureAwait(false);
            }
        }

        public List<string> Languages() => new List<string>
        {
            DefaultLanguage,
            "arabic",
            "bulgarian",
            "schinese",
            "tchinese",
            "czech",
            "danish",
            "dutch",
            "finnish",
            "french",
            "german",
            "greek",
            "hungarian",
            "italian",
            "japanese",
            "koreana",
            "norwegian",
            "polish",
            "portuguese",
            "brazilian",
            "romanian",
            "russian",
            "spanish",
            "swedish",
            "thai",
            "turkish",
            "ukrainian"
        };

        private void Copy(string source, string destination)
        {
            if (!Directory.Exists(destination))
            {
                Directory.CreateDirectory(destination);
            }

            foreach (var file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            }

            foreach (var directory in Directory.GetDirectories(source))
            {
                Copy(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }
        }

        private static bool FindInterfaces(ref HashSet<string> result, string dllContent, Regex regex)
        {
            var success = false;
            var matches = regex.Matches(dllContent);
            foreach (Match match in matches)
            {
                success = true;
                //result += $@"{match.Value}\n";
                result.Add(match.Value);
            }

            return success;
        }

        private async Task DownloadImageAsync(string imageFolder, string imageUrl)
        {
            var fileName = Path.GetFileName(imageUrl);
            var targetPath = Path.Combine(imageFolder, fileName);
            if (File.Exists(targetPath))
            {
                return;
            }
            else if (imageUrl.StartsWith("images/"))
            {
                _log.Warn($"Previously downloaded image '{imageUrl}' is now missing!");
            }

            using var client = new HttpClient();
            var response = await client.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();
            await using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fileStream);
        }

        [GeneratedRegex(@"(?<id>.*) *= *(?<name>.*)")]
        private static partial Regex MyRegex();
    }
}