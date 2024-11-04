using OgreEngine;
using System.Windows;


namespace OgrePF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            App.Current.Exit += Current_Exit;

            InitializeComponent();
        }

        void Current_Exit(object sender, ExitEventArgs e)
        {
            RenterTargetControl.Source = null;

            ogreImage.Dispose();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ogreImage = (OgreImage)RenterTargetControl.Source;
            ogreImage.InitOgreAsync();

        }
    }
}
