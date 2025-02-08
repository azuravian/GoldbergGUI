using MvvmCross.Exceptions;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System.Threading.Tasks;

namespace GoldbergGUI.Core.Utils
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class CustomMvxAppStart<TViewModel> : MvxAppStart<TViewModel> where TViewModel : IMvxViewModel
    {
        public CustomMvxAppStart(IMvxApplication application, IMvxNavigationService navigationService) : base(application, navigationService)
        {
        }

        protected override async Task NavigateToFirstViewModel(object hint = null)
        {
            //return base.NavigateToFirstViewModel(hint);
            try
            {
                await NavigationService.Navigate<TViewModel>().ConfigureAwait(false);
            }
            catch (System.Exception exception)
            {
                throw exception.MvxWrap("Problem navigating to ViewModel {0}", typeof(TViewModel).Name);
            }
        }
    }
}