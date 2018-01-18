using System;

using IoTPlayer.ViewModels;

using Windows.UI.Xaml.Controls;

namespace IoTPlayer.Views
{
    public sealed partial class MainPage : Page
    {
        private MainViewModel ViewModel
        {
            get { return DataContext as MainViewModel; }
        }

        public MainPage()
        {
            InitializeComponent();
        }
    }
}
