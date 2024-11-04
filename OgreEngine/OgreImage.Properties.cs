using System;
using System.Windows;

namespace OgreEngine
{
    public partial class OgreImage
    {
        public static readonly DependencyProperty ViewportSizeProperty =
            DependencyProperty.Register("ViewportSize", typeof(Size), typeof(OgreImage),
                                        new PropertyMetadata(new Size(100, 100), OnViewportProperyChanged)
                );

        private static void OnViewportProperyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var imageSource = (OgreImage)d;

            imageSource.reloadRenderTargetTime = Environment.TickCount;
        }

        public Size ViewportSize
        {
            get { return (Size)GetValue(ViewportSizeProperty); }
            set { SetValue(ViewportSizeProperty, value); }
        }

        public static readonly DependencyProperty AutoInitialiseProperty =
            DependencyProperty.Register("AutoInitialise", typeof(bool), typeof(OgreImage),
                                        new PropertyMetadata(false));

        public bool AutoInitialise
        {
            get { return (bool)GetValue(AutoInitialiseProperty); }
            set { SetValue(AutoInitialiseProperty, value); }
        }


        public static readonly DependencyProperty CreateDefaultSceneProperty =
            DependencyProperty.Register("CreateDefaultScene", typeof(bool), typeof(OgreImage),
                                        new PropertyMetadata(true));

        public bool CreateDefaultScene
        {
            get { return (bool)GetValue(CreateDefaultSceneProperty); }
            set { SetValue(CreateDefaultSceneProperty, value); }
        }

        public static readonly DependencyProperty ResizeRenderTargetDelayProperty =
            DependencyProperty.Register("ResizeRenderTargetDelay", typeof(Duration), typeof(OgreImage),
            new PropertyMetadata(new Duration(new TimeSpan(200))));

        public Duration ResizeRenderTargetDelay
        {
            get { return (Duration)GetValue(ResizeRenderTargetDelayProperty); }
            set { SetValue(ResizeRenderTargetDelayProperty, value); }
        }

        public static readonly DependencyProperty FrameRateProperty =
            DependencyProperty.Register("FrameRate", typeof(int?), typeof(OgreImage),
            new PropertyMetadata(FrameRate_Changed));

        private static void FrameRate_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((OgreImage)d).OnFrameRateChanged((int?)e.NewValue);
        }

        public int? FrameRate
        {
            get { return (int?)GetValue(FrameRateProperty); }
            set { SetValue(FrameRateProperty, value); }
        }
    }
}