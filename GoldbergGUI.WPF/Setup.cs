using MvvmCross.Logging;
using MvvmCross.Platforms.Wpf.Core;
using Serilog;
using System.IO;

namespace GoldbergGUI.WPF
{
    public class Setup : MvxWpfSetup<Core.App>
    {
        public override MvxLogProviderType GetDefaultLogProviderType() => MvxLogProviderType.Serilog;

        protected override IMvxLogProvider CreateLogProvider()
        {
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "goldberg_.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
            return base.CreateLogProvider();
        }

    }
}