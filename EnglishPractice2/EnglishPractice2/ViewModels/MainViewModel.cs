using System;
using System.Windows.Input;
using EnglishPractice2.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Views;

namespace EnglishPractice2.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string _showText;
        private string _result;

        /// <summary>
        /// 시작 여부 확인
        /// </summary>
        private bool _hasStart;
        /// <summary>
        /// 난수 생성용
        /// </summary>
        private Random _random;

           
        /// <summary>
        /// 기본 생성자
        /// </summary>
        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                ShowText = "내 커피를 만들어라";
                Result = "실패!! 잘 듣고 다시 따라 하세요";
            }
            else
            {
                Init();
            }
        }

        /// <summary>
        /// 네비게이션 서비스
        /// </summary>
        private NavigationServiceEx NavigationService
            => SimpleIoc.Default.GetInstance<NavigationServiceEx>();

        /// <summary>
        /// 초기화
        /// </summary>
        private void Init()
        {
            NavigationService.Navigated += NavigationService_Navigated;

            StartCommand = new RelayCommand(StartCommandExecute);
            StopCommand = new RelayCommand(() =>
            {

            });
            MediaEndedCommand = new RelayCommand(() =>
            {

            });
        }

        /// <summary>
        /// 네비게이트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavigationService_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            //랜덤 초기화
            _random = new Random();
            
        }

        /// <summary>
        /// 시작 버튼 클릭시 실행할 메소드
        /// </summary>
        private void StartCommandExecute()
        {
            //출력할 단문 하나 랜덤 선택
        }

        /// <summary>
        /// 시작
        /// </summary>
        public ICommand StartCommand { get; set; }
        /// <summary>
        /// 종료
        /// </summary>
        public ICommand StopCommand { get; set; }
        /// <summary>
        /// 미디어 종료 커맨드
        /// </summary>
        public ICommand MediaEndedCommand { get; set; }

        /// <summary>
        /// 발음 해야할 단문
        /// </summary>
        public string ShowText
        {
            get { return _showText; }
            set { Set(ref _showText ,value); }
        }
        /// <summary>
        /// 결과
        /// </summary>
        public string Result
        {
            get { return _result; }
            set { Set(ref _result ,value); }
        }
    }
}
