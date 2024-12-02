using OgreEngine;
using System.Windows;
using System.Windows.Input;

namespace OgrePF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OgreImage d3dImage;

        private bool isCtrlPressed = false;

        private bool isLeftMouseButtonClicked = false, isRightMouseButtonClicked = false;

        private double lastXAxis, lastYAxis;

        public MainWindow()
        {
            InitializeComponent();
            d3dImage = new OgreImage();
            this.PreviewKeyDown += Window_PreviewKeyUp;
            this.PreviewKeyUp += Window_PreviewKeyDown;
            this.MouseWheel += Window_MouseWheel;
            lastXAxis = 0;
            lastYAxis = 0;
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
                updateOgre();
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
                    d3dImage.setCameraDistance(true);                    

                }
                else
                {
                    d3dImage.setCameraDistance(false);
                }

                updateOgre();
            }
        }

        // Do after any event that change Ogre
        private void updateOgre()
        {
            d3dImage.InitRenderTarget();
            d3dImage.AttachRenderTarget();
            d3dImage.RenderOneFrame();
        }

        private void img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isLeftMouseButtonClicked = true;
        }

        private void img_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            isRightMouseButtonClicked = true;
        }

        private void img_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            isRightMouseButtonClicked = false;
        }

        private void img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isLeftMouseButtonClicked = false;
        }

        private void img_MouseMove(object sender, MouseEventArgs e)
        {
            double xAxis = e.GetPosition(img).X;
            double yAxis = e.GetPosition(img).Y;

            if (isLeftMouseButtonClicked)
            {
                bool growX;                                            

                if((xAxis - lastXAxis) > 0)
                {
                    growX = true;
                }else 
                {
                    growX = false;
                }                

                d3dImage.setCameraAngleX(growX);

                updateOgre();
            }

            if(isRightMouseButtonClicked)
            {
                bool growY;

                if ((yAxis - lastYAxis) > 0)
                {
                    growY = true;
                }
                else
                {
                    growY = false;
                }

                d3dImage.setCameraAngleY(growY);

                updateOgre();
            }

            lastXAxis = xAxis;
            lastYAxis = yAxis;
        }
    }
}
