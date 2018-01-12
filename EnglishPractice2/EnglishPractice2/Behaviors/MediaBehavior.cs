using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace EnglishPractice2.Behaviors
{
    public class MediaBehavior : Behavior<MediaElement>
    {
        public SpeechSynthesisStream Stream
        {
            get { return (SpeechSynthesisStream)GetValue(StreamProperty); }
            set { SetValue(StreamProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Stream.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StreamProperty =
            DependencyProperty.Register("Stream", typeof(SpeechSynthesisStream), typeof(MediaBehavior)
                , new PropertyMetadata(null, StreamChanged));

        private static void StreamChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (MediaBehavior) d;
            behavior.ExecuteStreamChanged();
        }

        private void ExecuteStreamChanged()
        {
            if (Stream == null) return;
            AssociatedObject.AutoPlay = true;
            AssociatedObject.SetSource(Stream, Stream.ContentType);
            AssociatedObject.Play();
        }


        protected override void OnAttached()
        {
            AssociatedObject.MediaEnded += AssociatedObject_MediaEnded            ;
        }

        private void AssociatedObject_MediaEnded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.AutoPlay = false;
            AssociatedObject.Stop();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MediaEnded -= AssociatedObject_MediaEnded;
        }
    }
}
