using org.ogre;
using System;
using System.Windows;
using System.Windows.Media;

namespace OgrePF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OgreSource.Initialize();
            OgreSource.InitRenderTarget();
            OgreSource.AttachRenderTarget();
            CompositionTarget.Rendering += OnRendering;
        }
        private void OnRendering(object sender, EventArgs e)
        {
            OgreSource.RenderLoop();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            OgreSource.Dispose();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OgreImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            OgreSource.ViewportSize = e.NewSize;
        }
    }
}
