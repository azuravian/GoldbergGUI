using GoldbergGUI.Core.Models;
using GoldbergGUI.Core.Services;
using GoldbergGUI.Core.Utils;
using MvvmCross.Commands;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows;


namespace GoldbergGUI.Core.ViewModels
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MainViewModel : MvxNavigationViewModel
    {
        private readonly IMvxNavigationService _navigationService;
        private string _dllPath;
        private string _gameName;

        private int _appId;

        //private SteamApp _currentGame;
        private ObservableCollection<Achievement> _achievements;
        private ObservableCollection<DlcApp> _dlcs;
        private string _accountName;
        private long _steamId;
        private bool _experimental;
        private bool _experimentalnow;
        private bool _offline;
        private bool _disableNetworking;
        private bool _disableOverlay;

        private string _statusText;

        private readonly ISteamService _steam;
        private readonly IGoldbergService _goldberg;
        private readonly IMvxLog _log;
        private bool _mainWindowEnabled;
        private bool _goldbergApplied;
        private ObservableCollection<string> _steamLanguages;
        private string _selectedLanguage;
        private readonly IMvxLogProvider _logProvider;

        public MainViewModel(ISteamService steam, IGoldbergService goldberg, IMvxLogProvider logProvider,
            IMvxNavigationService navigationService) : base(logProvider, navigationService)
        {
            _steam = steam;
            _goldberg = goldberg;
            _logProvider = logProvider;
            _log = logProvider.GetLogFor<MainViewModel>();
            _navigationService = navigationService;
        }

        public override void Prepare()
        {
#if DEBUG
            if(!Debugger.IsAttached) Debugger.Launch();
#endif
            base.Prepare();
            Task.Run(async () =>
            {
                //var errorDuringInit = false;
                MainWindowEnabled = false;
                StatusText = "Initializing! Please wait...";
                try
                {
                    SteamLanguages = new ObservableCollection<string>(_goldberg.Languages());
                    ResetForm();
                    await _steam.Initialize(_logProvider.GetLogFor<SteamService>()).ConfigureAwait(false);
                    await _goldberg.Initialize(_logProvider.GetLogFor<GoldbergService>()).ConfigureAwait(false);
                    var globalConfiguration =
                        await _goldberg.GetGlobalSettings().ConfigureAwait(false);
                    AccountName = globalConfiguration.AccountName;
                    SteamId = globalConfiguration.UserSteamId;
                    SelectedLanguage = globalConfiguration.Language;
                    Experimental = globalConfiguration.Experimental;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    _log.Error(e.Message);
                    //throw;
                }

                MainWindowEnabled = true;
                StatusText = "Ready.";
            });
        }

        public override async Task Initialize()
        {
            await base.Initialize().ConfigureAwait(false);
        }

        // PROPERTIES //

        public string DllPath
        {
            get => _dllPath;
            private set
            {
                _dllPath = value;
                RaisePropertyChanged(() => DllPath);
                RaisePropertyChanged(() => DllSelected);
                RaisePropertyChanged(() => SteamInterfacesTxtExists);
            }
        }

        public string GameName
        {
            get => _gameName;
            set
            {
                _gameName = value;
                RaisePropertyChanged(() => GameName);
            }
        }

        public int AppId
        {
            get => _appId;
            set
            {
                _appId = value;
                RaisePropertyChanged(() => AppId);
                Task.Run(async () => await GetNameById().ConfigureAwait(false));
            }
        }

        // ReSharper disable once InconsistentNaming
        public ObservableCollection<DlcApp> DLCs
        {
            get => _dlcs;
            set
            {
                _dlcs = value;
                RaisePropertyChanged(() => DLCs);
                /*RaisePropertyChanged(() => DllSelected);
                RaisePropertyChanged(() => SteamInterfacesTxtExists);*/
            }
        }

        public ObservableCollection<Achievement> Achievements
        {
            get => _achievements;
            set
            {
                _achievements = value;
                RaisePropertyChanged(() => Achievements);
            }
        }

        public string AccountName
        {
            get => _accountName;
            set
            {
                _accountName = value;
                RaisePropertyChanged(() => AccountName);
            }
        }

        public long SteamId
        {
            get => _steamId;
            set
            {
                _steamId = value;
                RaisePropertyChanged(() => SteamId);
            }
        }

        public bool Experimental
        {
            get => _experimental;
            set
            {
                _experimental = value;
                RaisePropertyChanged(() => Experimental);
            }
        }

        public bool ExperimentalNow
        {
            get => _experimentalnow;
            set
            {
                _experimentalnow = value;
                RaisePropertyChanged(() => ExperimentalNow);
            }
        }

        public bool Offline
        {
            get => _offline;
            set
            {
                _offline = value;
                RaisePropertyChanged(() => Offline);
            }
        }

        public bool DisableNetworking
        {
            get => _disableNetworking;
            set
            {
                _disableNetworking = value;
                RaisePropertyChanged(() => DisableNetworking);
            }
        }

        public bool DisableOverlay
        {
            get => _disableOverlay;
            set
            {
                _disableOverlay = value;
                RaisePropertyChanged(() => DisableOverlay);
            }
        }

        public bool MainWindowEnabled
        {
            get => _mainWindowEnabled;
            set
            {
                _mainWindowEnabled = value;
                RaisePropertyChanged(() => MainWindowEnabled);
            }
        }

        public bool GoldbergApplied
        {
            get => _goldbergApplied;
            set
            {
                _goldbergApplied = value;
                RaisePropertyChanged(() => GoldbergApplied);
            }
        }

        public bool SteamInterfacesTxtExists
        {
            get
            {
                var dllPathDirExists = GetDllPathDir(out var dirPath);
                return dllPathDirExists && !File.Exists(Path.Combine(dirPath, "steam_interfaces.txt"));
            }
        }

        public bool DllSelected
        {
            get
            {
                var value = !DllPath.Contains("Path to game's steam_api(64).dll");
                if (!value) _log.Warn("No DLL selected! Skipping...");
                return value;
            }
        }

        public ObservableCollection<string> SteamLanguages
        {
            get => _steamLanguages;
            set
            {
                _steamLanguages = value;
                RaisePropertyChanged(() => SteamLanguages);
            }
        }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value;
                RaisePropertyChanged(() => SelectedLanguage);
                //MyLogger.Log.Debug($"Lang: {value}");
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                RaisePropertyChanged(() => StatusText);
            }
        }

        public static string AboutVersionText =>
            FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

        public static GlobalHelp G => new GlobalHelp();

        // COMMANDS //

        public IMvxCommand OpenFileCommand => new MvxAsyncCommand(OpenFile);

        private async Task OpenFile()
        {
            MainWindowEnabled = false;
            StatusText = "Please choose a file...";
            var dialog = new OpenFileDialog
            {
                Filter = "SteamAPI DLL|steam_api.dll;steam_api64.dll|" +
                         "All files (*.*)|*.*",
                Multiselect = false,
                Title = "Select SteamAPI DLL..."
            };
            if (dialog.ShowDialog() != true)
            {
                MainWindowEnabled = true;
                _log.Warn("File selection canceled.");
                StatusText = "No file selected! Ready.";
                return;
            }

            DllPath = dialog.FileName;
            await ReadConfig().ConfigureAwait(false);
            if (!GoldbergApplied) await GetListOfDlc().ConfigureAwait(false);
            MainWindowEnabled = true;
            StatusText = "Ready.";
        }

        public IMvxCommand FindIdCommand => new MvxAsyncCommand(FindId);

        private async Task FindId()
        {
            async Task FindIdInList(SteamApp[] steamApps)
            {
                var navigateTask = _navigationService
                    .Navigate<SearchResultViewModel, IEnumerable<SteamApp>, SteamApp>(steamApps);
                var navigateResult = await navigateTask.ConfigureAwait(false);
                if (navigateResult != null)
                {
                    GameName = navigateResult.Name;
                    AppId = navigateResult.AppId;
                }
            }

            if (GameName.Contains("Game name..."))
            {
                _log.Error("No game name entered!");
                return;
            }

            MainWindowEnabled = false;
            StatusText = "Trying to find AppID...";
            var appByName = await _steam.GetAppByName(_gameName).ConfigureAwait(false);
            if (appByName != null)
            {
                GameName = appByName.Name;
                AppId = appByName.AppId;
            }
            else
            {
                var list = await _steam.GetListOfAppsByName(GameName).ConfigureAwait(false);
                var steamApps = list as SteamApp[] ?? list.ToArray();
                if (steamApps.Length == 1)
                {
                    var steamApp = steamApps[0];
                    if (steamApp != null)
                    {
                        GameName = steamApp.Name;
                        AppId = steamApp.AppId;
                    }
                    else
                    {
                        await FindIdInList(steamApps).ConfigureAwait(false);
                    }
                }
                else
                {
                    await FindIdInList(steamApps).ConfigureAwait(false);
                }
            }
            await GetListOfDlc().ConfigureAwait(false);
            MainWindowEnabled = true;
            StatusText = "Ready.";
        }

        //public IMvxCommand GetNameByIdCommand => new MvxAsyncCommand(GetNameById);

        private async Task GetNameById()
        {
            if (AppId <= 0)
            {
                _log.Error("Invalid Steam App!");
                return;
            }

            var steamApp = await _steam.GetAppById(AppId).ConfigureAwait(false);
            if (steamApp != null) GameName = steamApp.Name;
        }

        public IMvxCommand GetListOfAchievementsCommand => new MvxAsyncCommand(GetListOfAchievements);

        private async Task GetListOfAchievements()
        {
            if (AppId <= 0)
            {
                _log.Error("Invalid Steam App!");
                return;
            }

            MainWindowEnabled = false;
            StatusText = "Trying to get list of achievements...";
            var listOfAchievements = await _steam.GetListOfAchievements(new SteamApp { AppId = AppId, Name = GameName });
            Achievements = new MvxObservableCollection<Achievement>(listOfAchievements);
            MainWindowEnabled = true;

            if (Achievements.Count > 0)
            {
                var empty = Achievements.Count == 1 ? "" : "s";
                StatusText = $"Successfully got {Achievements.Count} achievement{empty}! Ready.";
            }
            else
            {
                StatusText = "No achievements found! Ready.";
            }
        }

        public IMvxCommand GetListOfDlcCommand => new MvxAsyncCommand(GetListOfDlc);

        private async Task GetListOfDlc()
        {
            if (AppId <= 0)
            {
                _log.Error("Invalid Steam App!");
                return;
            }

            MainWindowEnabled = false;
            StatusText = "Trying to get list of DLCs...";
            var listOfDlc = await _steam.GetListOfDlc(new SteamApp { AppId = AppId, Name = GameName }, true)
                .ConfigureAwait(false);
            DLCs = new MvxObservableCollection<DlcApp>(listOfDlc);
            MainWindowEnabled = true;
            if (DLCs.Count > 0)
            {
                var empty = DLCs.Count == 1 ? "" : "s";
                StatusText = $"Successfully got {DLCs.Count} DLC{empty}! Ready.";
            }
            else
            {
                StatusText = "No DLC found! Ready.";
            }
        }

        public IMvxCommand SaveConfigCommand => new MvxAsyncCommand(SaveConfig);

        private async Task SaveConfig()
        {
            _log.Info("Saving global settings...");
            var globalConfiguration = new GoldbergGlobalConfiguration
            {
                AccountName = AccountName,
                UserSteamId = SteamId,
                Language = SelectedLanguage,
                Experimental = Experimental
            };
            await _goldberg.SetGlobalSettings(globalConfiguration).ConfigureAwait(false);
            if (!DllSelected) return;

            _log.Info("Saving Goldberg settings...");
            if (!GetDllPathDir(out var dirPath)) return;
            MainWindowEnabled = false;
            StatusText = "Saving...";
            await _goldberg.Save(dirPath, new GoldbergConfiguration
            {
                AppId = AppId,
                Achievements = Achievements.ToList(),
                DlcList = DLCs.ToList(),
                Offline = Offline,
                ExperimentalNow = ExperimentalNow,
                DisableNetworking = DisableNetworking,
                DisableOverlay = DisableOverlay
            }
            ).ConfigureAwait(false);
            GoldbergApplied = _goldberg.GoldbergApplied(dirPath);
            MainWindowEnabled = true;
            StatusText = "Ready.";
        }

        public IMvxCommand ResetConfigCommand => new MvxAsyncCommand(ResetConfig);

        private async Task ResetConfig()
        {
            var globalConfiguration = await _goldberg.GetGlobalSettings().ConfigureAwait(false);
            AccountName = globalConfiguration.AccountName;
            SteamId = globalConfiguration.UserSteamId;
            SelectedLanguage = globalConfiguration.Language;
            Experimental = globalConfiguration.Experimental;
            if (!DllSelected) return;

            _log.Info("Reset form...");
            MainWindowEnabled = false;
            StatusText = "Resetting...";
            await ReadConfig().ConfigureAwait(false);
            MainWindowEnabled = true;
            StatusText = "Ready.";
        }

        public IMvxCommand GenerateSteamInterfacesCommand => new MvxAsyncCommand(GenerateSteamInterfaces);

        private async Task GenerateSteamInterfaces()
        {
            if (!DllSelected) return;

            _log.Info("Generate steam_interfaces.txt...");
            MainWindowEnabled = false;
            StatusText = @"Generating ""steam_interfaces.txt"".";
            GetDllPathDir(out var dirPath);
            if (File.Exists(Path.Combine(dirPath, "steam_api_o.dll")))
                await _goldberg.GenerateInterfacesFile(Path.Combine(dirPath, "steam_api_o.dll")).ConfigureAwait(false);
            else if (File.Exists(Path.Combine(dirPath, "steam_api64_o.dll")))
                await _goldberg.GenerateInterfacesFile(Path.Combine(dirPath, "steam_api64_o.dll"))
                    .ConfigureAwait(false);
            else await _goldberg.GenerateInterfacesFile(DllPath).ConfigureAwait(false);
            await RaisePropertyChanged(() => SteamInterfacesTxtExists).ConfigureAwait(false);
            MainWindowEnabled = true;
            StatusText = "Ready.";
        }

        public IMvxCommand PasteDlcCommand => new MvxCommand(() =>
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            _log.Info("Trying to paste DLC list...");
            if (!(Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text)))
            {
                _log.Warn("Invalid DLC list!");
            }
            else
            {
                var result = Clipboard.GetText();
                var expression = new Regex(@"(?<id>.*) *= *(?<name>.*)");
                var pastedDlc = (from line in result.Split(new[] { "\n", "\r\n" },
                    StringSplitOptions.RemoveEmptyEntries)
                                 select expression.Match(line) into match
                                 where match.Success
                                 select new DlcApp
                                 {
                                     AppId = Convert.ToInt32(match.Groups["id"].Value),
                                     Name = match.Groups["name"].Value
                                 }).ToList();
                if (pastedDlc.Count > 0)
                {
                    DLCs.Clear();
                    DLCs = new ObservableCollection<DlcApp>(pastedDlc);
                    //var empty = DLCs.Count == 1 ? "" : "s";
                    //StatusText = $"Successfully got {DLCs.Count} DLC{empty} from clipboard! Ready.";
                    var statusTextCount = DLCs.Count == 1 ? "one DLC" : $"{DLCs.Count} DLCs";
                    StatusText = $"Successfully got {statusTextCount} from clipboard! Ready.";
                }
                else
                {
                    StatusText = "No DLC found in clipboard! Ready.";
                }
            }
        });

        public IMvxCommand OpenGlobalSettingsFolderCommand => new MvxCommand(OpenGlobalSettingsFolder);

        private void OpenGlobalSettingsFolder()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                StatusText = "Can't open folder (Windows only)! Ready.";
                return;
            }

            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Goldberg SteamEmu Saves", "settings");
            var start = Process.Start("explorer.exe", path);
            start?.Dispose();
        }

        // OTHER METHODS //

        private void ResetForm()
        {
            DllPath = "Path to game's steam_api(64).dll...";
            GameName = "Game name...";
            AppId = -1;
            Achievements = new ObservableCollection<Achievement>();
            DLCs = new ObservableCollection<DlcApp>();
            AccountName = "Account name...";
            SteamId = -1;
            Experimental = false;
            Offline = false;
            DisableNetworking = false;
            DisableOverlay = false;
        }

        private async Task ReadConfig()
        {
            if (!GetDllPathDir(out var dirPath)) return;
            var config = await _goldberg.Read(dirPath).ConfigureAwait(false);
            SetFormFromConfig(config);
            GoldbergApplied = _goldberg.GoldbergApplied(dirPath);
            await RaisePropertyChanged(() => SteamInterfacesTxtExists).ConfigureAwait(false);
        }

        private void SetFormFromConfig(GoldbergConfiguration config)
        {
            AppId = config.AppId;
            Achievements = new ObservableCollection<Achievement>(config.Achievements);
            DLCs = new ObservableCollection<DlcApp>(config.DlcList);
            ExperimentalNow = config.ExperimentalNow;
            Offline = config.Offline;
            DisableNetworking = config.DisableNetworking;
            DisableOverlay = config.DisableOverlay;
        }

        private bool GetDllPathDir(out string dirPath)
        {
            if (!DllSelected)
            {
                dirPath = null;
                return false;
            }

            dirPath = Path.GetDirectoryName(DllPath);
            if (dirPath != null) return true;

            _log.Error($"Invalid directory for {DllPath}.");
            return false;
        }
    }
}