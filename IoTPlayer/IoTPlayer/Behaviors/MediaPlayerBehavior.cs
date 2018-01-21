using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using GalaSoft.MvvmLight.Messaging;
using IoTPlayer.Commons;
using Microsoft.Xaml.Interactivity;

namespace IoTPlayer.Behaviors
{
    /// <summary>
    /// 미디어플레이어 비헤이비어
    /// </summary>
    public class MediaPlayerBehavior : Behavior<MediaPlayerElement>
    {
        public MediaPlaybackState CurrentPlaybackState
        {
            get { return (MediaPlaybackState)GetValue(CurrentPlaybackStateProperty); }
            set { SetValue(CurrentPlaybackStateProperty, value); }
        }
        /// <summary>
        /// 현재 상태
        /// </summary>
        public static readonly DependencyProperty CurrentPlaybackStateProperty =
            DependencyProperty.Register("CurrentPlaybackState", typeof(MediaPlaybackState)
                , typeof(MediaPlayerBehavior), new PropertyMetadata(MediaPlaybackState.None));


        public IMediaPlaybackSource Source
        {
            get { return (IMediaPlaybackSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// 미디어 소스
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(IMediaPlaybackSource), typeof(MediaPlayerBehavior), new PropertyMetadata(null, SourceChanged));



        public ICommand MediaCommand
        {
            get { return (ICommand)GetValue(MediaCommandProperty); }
            set { SetValue(MediaCommandProperty, value); }
        }

        /// <summary>
        /// 미디어 커맨드
        /// </summary>
        public static readonly DependencyProperty MediaCommandProperty =
            DependencyProperty.Register("MediaCommand", typeof(ICommand), typeof(MediaPlayerBehavior), new PropertyMetadata(null));


        private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (MediaPlayerBehavior) d;
            behavior.ExecuteSourceChanged();
        }

        private void ExecuteSourceChanged()
        {
            AssociatedObject.MediaPlayer.Source = Source;
        }


        protected override void OnAttached()
        {
            if(AssociatedObject == null)
                throw new NullReferenceException();

            if (AssociatedObject.MediaPlayer == null)
            {
                AssociatedObject.SetMediaPlayer(new MediaPlayer());
            }

            AssociatedObject.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            AssociatedObject.MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            AssociatedObject.MediaPlayer.MediaOpened += MediaPlayer_MediaOpened;


            AssociatedObject.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            //CommandMediaPlayer 데이터 수신 등록
            Messenger.Default.Register<CommandMediaPlayer>(this, ExecuteCommandMediaPlayer);
        }

        private void ExecuteCommandMediaPlayer(CommandMediaPlayer command)
        {
            switch (command)
            {
                case CommandMediaPlayer.None:
                    break;
                case CommandMediaPlayer.Play:
                    if (Source == null) return;
                    AssociatedObject.MediaPlayer.Play();
                    break;
                case CommandMediaPlayer.Pause:
                    if (Source == null) return;
                    AssociatedObject.MediaPlayer.Pause();
                    break;
                case CommandMediaPlayer.Previous:
                case CommandMediaPlayer.Next:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command, null);
            }
        }

        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            await AssociatedObject.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                CurrentPlaybackState = AssociatedObject.MediaPlayer.PlaybackSession.PlaybackState;
            });
        }

        private async void MediaPlayer_MediaOpened(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            await AssociatedObject.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MediaCommand?.Execute(ResultMediaPlayer.Opened);
            });
        }

        private async void MediaPlayer_MediaFailed(Windows.Media.Playback.MediaPlayer sender, Windows.Media.Playback.MediaPlayerFailedEventArgs args)
        {
            await AssociatedObject.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MediaCommand?.Execute(ResultMediaPlayer.Failed);
            });
        }

        private async void MediaPlayer_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            await AssociatedObject.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MediaCommand?.Execute(ResultMediaPlayer.Ended);
            });
        }

        protected override void OnDetaching()
        {
            //CommandMediaPlayer 데이터 수신 해제
            Messenger.Default.Unregister<CommandMediaPlayer>(this, ExecuteCommandMediaPlayer);

            AssociatedObject.MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            AssociatedObject.MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
            AssociatedObject.MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
            AssociatedObject.MediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
        }
    }
}
