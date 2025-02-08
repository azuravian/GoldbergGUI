using GoldbergGUI.Core.Utils;
using GoldbergGUI.Core.ViewModels;
using MvvmCross.IoC;
using MvvmCross.ViewModels;

namespace GoldbergGUI.Core
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            CreatableTypes()
                .EndingWith("Service")
                .AsInterfaces()
                .RegisterAsLazySingleton();
            //RegisterAppStart<MainViewModel>();
            RegisterCustomAppStart<CustomMvxAppStart<MainViewModel>>();
        }
    }
}