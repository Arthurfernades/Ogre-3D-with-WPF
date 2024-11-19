using org.ogre;
using SharpDXLib;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace OgrePF
{
    /// <summary>
    /// Interaction logic for SharpDXWindow.xaml
    /// </summary>
    public partial class SharpDXWindow : Window
    {

        private OgreRender _renderer;
        private D3DImageRenderer _d3dImage;

        public SharpDXWindow()
        {
            InitializeComponent();

            // Inicialize o renderizador Ogre3D
            _renderer = new OgreRender();
            _renderer.Initialize(800, 600);

            // Inicialize o D3DImage
            _d3dImage = new D3DImageRenderer();
            _d3dImage.Initialize(_renderer.SharedHandle);

            // Vincule o D3DImage ao WPF
            System.Windows.Controls.Image imageControl = new System.Windows.Controls.Image
            {
                Source = _d3dImage,
                Stretch = Stretch.None,
                Width = 800,
                Height = 600
            };
            MainGrid.Children.Add(imageControl);

            // Atualizar o frame
            CompositionTarget.Rendering += (s, e) => UpdateFrame();
        }

        private void UpdateFrame()
        {
            _renderer.RenderFrame();
            _d3dImage.UpdateFrame();
        }
    }
}
