using GoldbergGUI.Core.Models;
using MvvmCross.Commands;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoldbergGUI.Core.ViewModels
{
    public class SearchResultViewModel : MvxNavigationViewModel<IEnumerable<SteamApp>>, IMvxViewModel<IEnumerable<SteamApp>, SteamApp>
    {
        private readonly IMvxNavigationService _navigationService;
        private readonly IMvxLog _log;
        private IEnumerable<SteamApp> _apps;

        public SearchResultViewModel(IMvxLogProvider logProvider, IMvxNavigationService navigationService) :
            base(logProvider, navigationService)
        {
            _log = logProvider.GetLogFor(typeof(SearchResultViewModel));
            _navigationService = navigationService;
        }

        public override void Prepare(IEnumerable<SteamApp> parameter)
        {
            Apps = parameter;
        }

        public IEnumerable<SteamApp> Apps
        {
            get => _apps;
            set
            {
                _apps = value;
                RaisePropertyChanged(() => Apps);
            }
        }

        public SteamApp Selected
        {
            get;
            set;
        }

        public IMvxCommand SaveCommand => new MvxAsyncCommand(Save);

        public IMvxCommand CloseCommand => new MvxAsyncCommand(Close);

        public TaskCompletionSource<object> CloseCompletionSource { get; set; }

        public override void ViewDestroy(bool viewFinishing = true)
        {
            if (viewFinishing && CloseCompletionSource != null && !CloseCompletionSource.Task.IsCompleted &&
                !CloseCompletionSource.Task.IsFaulted)
                CloseCompletionSource?.TrySetCanceled();

            base.ViewDestroy(viewFinishing);
        }

        private async Task Save()
        {
            if (Selected != null)
            {
                _log.Info($"Successfully got app {Selected}");
                await _navigationService.Close(this, Selected).ConfigureAwait(false);
            }
        }

        private async Task Close()
        {
            await _navigationService.Close(this).ConfigureAwait(false);
        }
    }
}