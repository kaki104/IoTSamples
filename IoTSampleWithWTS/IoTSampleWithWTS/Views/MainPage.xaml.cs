using System;

using IoTSampleWithWTS.ViewModels;

using Windows.UI.Xaml.Controls;

namespace IoTSampleWithWTS.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel => DataContext as MainViewModel;

        public MainPage()
        {
            InitializeComponent();
        }

        
    }
}
