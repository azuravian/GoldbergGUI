using MvvmCross.Platforms.Wpf.Presenters.Attributes;

// ReSharper disable UnusedType.Global
namespace GoldbergGUI.WPF.Views
{
    [MvxWindowPresentation(Identifier = nameof(SearchResultView), Modal = false)]
    public partial class SearchResultView
    {
        public SearchResultView()
        {
            InitializeComponent();
        }
    }
}