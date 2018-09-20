using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using IoTPlayer.Helpers;
using IoTPlayer.Services;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Toolkit.Uwp.UI.Controls;

namespace IoTPlayer.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        private const string PANORAMIC_STATE_NAME = "PanoramicState";
        private const string WIDE_STATE_NAME = "WideState";
        private const string NARROW_STATE_NAME = "NarrowState";
        private const double WIDE_STATE_MIN_WINDOW_WIDTH = 640;
        private const double PANORAMIC_STATE_MIN_WINDOW_WIDTH = 1024;

        private SplitViewDisplayMode _displayMode = SplitViewDisplayMode.CompactInline;

        private bool _isPaneOpen;

        private ICommand _itemSelected;

        private object _lastSelectedItem;

        private ICommand _openPaneCommand;

        private ObservableCollection<ShellNavigationItem> _primaryItems =
            new ObservableCollection<ShellNavigationItem>();

        private ObservableCollection<ShellNavigationItem> _secondaryItems =
            new ObservableCollection<ShellNavigationItem>();

        private object _selected;

        private ICommand _stateChangedCommand;

        public NavigationServiceEx NavigationService => ServiceLocator.Current.GetInstance<NavigationServiceEx>();

        public bool IsPaneOpen
        {
            get => _isPaneOpen;
            set => Set(ref _isPaneOpen, value);
        }

        public object Selected
        {
            get => _selected;
            set => Set(ref _selected, value);
        }

        public SplitViewDisplayMode DisplayMode
        {
            get => _displayMode;
            set => Set(ref _displayMode, value);
        }

        public ObservableCollection<ShellNavigationItem> PrimaryItems
        {
            get => _primaryItems;
            set => Set(ref _primaryItems, value);
        }

        public ObservableCollection<ShellNavigationItem> SecondaryItems
        {
            get => _secondaryItems;
            set => Set(ref _secondaryItems, value);
        }

        public ICommand OpenPaneCommand
        {
            get
            {
                if (_openPaneCommand == null) _openPaneCommand = new RelayCommand(() => IsPaneOpen = !_isPaneOpen);

                return _openPaneCommand;
            }
        }

        public ICommand ItemSelectedCommand
        {
            get
            {
                if (_itemSelected == null)
                    _itemSelected = new RelayCommand<HamburgetMenuItemInvokedEventArgs>(ItemSelected);

                return _itemSelected;
            }
        }

        public ICommand StateChangedCommand
        {
            get
            {
                if (_stateChangedCommand == null)
                    _stateChangedCommand =
                        new RelayCommand<VisualStateChangedEventArgs>(args => GoToState(args.NewState.Name));

                return _stateChangedCommand;
            }
        }

        private void GoToState(string stateName)
        {
            switch (stateName)
            {
                case PANORAMIC_STATE_NAME:
                    DisplayMode = SplitViewDisplayMode.CompactInline;
                    break;
                case WIDE_STATE_NAME:
                    DisplayMode = SplitViewDisplayMode.CompactInline;
                    IsPaneOpen = false;
                    break;
                case NARROW_STATE_NAME:
                    DisplayMode = SplitViewDisplayMode.Overlay;
                    IsPaneOpen = false;
                    break;
                default:
                    break;
            }
        }

        public void Initialize(Frame frame)
        {
            NavigationService.Frame = frame;
            NavigationService.Navigated += Frame_Navigated;
            PopulateNavItems();

            InitializeState(Window.Current.Bounds.Width);

            //외장 디스크 연결 확인
            //todo : 이미 선택되어 있는 외장 폴더가 존재하는 경우에 확인하는 코드
            //todo : 일단 Music 라이브러리에 있는 파일을 읽어서 출력하는 코드 작성

            //Speech Recognition 초기화

            //SRGS 초기화

            //MediaPlayerElement 초기화
        }

        private void InitializeState(double windowWith)
        {
            if (windowWith < WIDE_STATE_MIN_WINDOW_WIDTH)
                GoToState(NARROW_STATE_NAME);
            else if (windowWith < PANORAMIC_STATE_MIN_WINDOW_WIDTH)
                GoToState(WIDE_STATE_NAME);
            else
                GoToState(PANORAMIC_STATE_NAME);
        }

        private void PopulateNavItems()
        {
            _primaryItems.Clear();
            _secondaryItems.Clear();

            // TODO WTS: Change the symbols for each item as appropriate for your app
            // More on Segoe UI Symbol icons: https://docs.microsoft.com/windows/uwp/style/segoe-ui-symbol-font
            // Or to use an IconElement instead of a Symbol see https://github.com/Microsoft/WindowsTemplateStudio/blob/master/docs/projectTypes/navigationpane.md
            // Edit String/en-US/Resources.resw: Add a menu item title for each page
            _primaryItems.Add(new ShellNavigationItem("Shell_MediaPlayer".GetLocalized(), Symbol.Document,
                typeof(MediaPlayerViewModel).FullName));
        }

        private void ItemSelected(HamburgetMenuItemInvokedEventArgs args)
        {
            if (DisplayMode == SplitViewDisplayMode.CompactOverlay || DisplayMode == SplitViewDisplayMode.Overlay)
                IsPaneOpen = false;

            Navigate(args.InvokedItem);
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e != null)
            {
                var vm = NavigationService.GetNameOfRegisteredPage(e.SourcePageType);
                var navigationItem = PrimaryItems?.FirstOrDefault(i => i.ViewModelName == vm) ?? SecondaryItems?.FirstOrDefault(i => i.ViewModelName == vm);

                if (navigationItem == null) return;
                ChangeSelected(_lastSelectedItem, navigationItem);
                _lastSelectedItem = navigationItem;
            }
        }

        private void ChangeSelected(object oldValue, object newValue)
        {
            if (oldValue != null) (oldValue as ShellNavigationItem).IsSelected = false;

            if (newValue != null)
            {
                (newValue as ShellNavigationItem).IsSelected = true;
                Selected = newValue;
            }
        }

        private void Navigate(object item)
        {
            var navigationItem = item as ShellNavigationItem;
            if (navigationItem != null) NavigationService.Navigate(navigationItem.ViewModelName);
        }
    }
}
