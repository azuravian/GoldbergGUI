using MvvmCross.Platforms.Wpf.Presenters.Attributes;

// ReSharper disable UnusedType.Global
namespace GoldbergGUI.WPF.Views
{
    /// <summary>
    ///     Interaction logic for MainView.xaml
    /// </summary>
    [MvxContentPresentation(WindowIdentifier = nameof(MainWindow))]
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();
        }
    }
}