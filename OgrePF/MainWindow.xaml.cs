using OgreEngine;
using org.ogre;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace OgrePF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OgreImage d3dImage;

        private bool isCtrlPressed = false;

        public MainWindow()
        {
            InitializeComponent();
            d3dImage = new OgreImage();
            this.PreviewKeyDown += Window_PreviewKeyUp;
            this.PreviewKeyUp += Window_PreviewKeyDown;
            this.MouseWheel += Window_MouseWheel;
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            img.Source = d3dImage;
            d3dImage.Initialize(true);

            UpdateOgreSize();
            d3dImage.CreateSceneDefault();
            d3dImage.RenderOneFrame();

        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            d3dImage.StopRendering();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            d3dImage.SalvaImagem();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.ActualWidth > 0 && this.ActualHeight > 0)
            {
                UpdateOgreSize();
            }
        }

        private void UpdateOgreSize()
        {
            if (d3dImage.isInited)
            {
                d3dImage.ViewportSize = new Size(this.ActualWidth - 20, this.ActualHeight - 20);
                d3dImage.InitRenderTarget();
                d3dImage.AttachRenderTarget();
                d3dImage.RenderOneFrame();
            }
        }


        private void Window_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                isCtrlPressed = true;
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                isCtrlPressed = false;
        }

        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (isCtrlPressed)
            {
                if (e.Delta > 0)
                {
                    d3dImage.setCameraDistance();
                }
                else
                {
                    // Scroll down
                    MessageBox.Show("Scroll down with Ctrl pressed! " + e.Delta);
                }
            }
        }

    }
}
