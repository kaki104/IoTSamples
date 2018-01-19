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
        }

        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            await AssociatedObject.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                CurrentPlaybackState = AssociatedObject.MediaPlayer.PlaybackSession.PlaybackState;
            });
        }

        private void MediaPlayer_MediaOpened(Windows.Media.Playback.MediaPlayer sender, object args)
        {
        }

        private void MediaPlayer_MediaFailed(Windows.Media.Playback.MediaPlayer sender, Windows.Media.Playback.MediaPlayerFailedEventArgs args)
        {
        }

        private void MediaPlayer_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            AssociatedObject.MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
            AssociatedObject.MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
            AssociatedObject.MediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
        }
    }
}
