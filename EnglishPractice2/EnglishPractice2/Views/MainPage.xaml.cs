using System;

using EnglishPractice2.ViewModels;

using Windows.UI.Xaml.Controls;

namespace EnglishPractice2.Views
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
