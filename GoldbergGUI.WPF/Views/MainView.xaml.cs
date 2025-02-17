using GoldbergGUI.Core.ViewModels;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using System.Windows;

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

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (null != e.Data && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                // Handle the dropped files
                // Example: Set the first file path to the DataContext's DllPath property
                if (files.Length > 0)
                {
                    e.Handled = true;
                    var viewModel = DataContext as MainViewModel;
                    if (viewModel != null)
                    {
                        _ = viewModel.OpenFile(files[0]);
                    }
                }
            }
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
    }
}