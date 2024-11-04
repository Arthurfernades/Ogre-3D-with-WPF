using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using WpfSharpDXLib;
using static System.Net.Mime.MediaTypeNames;

namespace SharpDXLib
{
    public class DSharpDxInterop
    {

        private Device device;

        private DeviceContext deviceContext;

        private DSharpDxInterop sharpDxInterop;

        private RenderTargetView RenderTargetView;

        private IntPtr lastResourcePointer = IntPtr.Zero;

        private RawMatrix projectionMatrix;

        private int surfaceWidth;

        private int surfaceHeight;

        private float farClip;

        private float nearClip;

        private float angleOfView;

        private readonly IGraphics graphics;

        public DSharpDxInterop(IGraphics graphics)
        {
            try
            {
                this.device = InitializeD3D11Device();
                this.deviceContext = device.ImmediateContext;
                this.graphics = graphics;
            }
            catch (Exception e)
            {
                throw new Exception("Could not initialize DSharpDXInterop" + e.Message);
            }
        }

        public bool Initialize(float angleOfViewDegrees, float nearClip, float farClip)
        {
            angleOfView = angleOfViewDegrees * (float)Math.PI / 180f;
            this.nearClip = nearClip;
            this.farClip = farClip;
            graphics.Initialize(device, deviceContext);
            return true;
        }


        public bool Render(IntPtr resourcePointer, bool isNewSurface, int width, int height)
        {
            // if the resource buffer changes : request the re-creation of the renderTargetView and viewPort
            if (resourcePointer != lastResourcePointer)
            {
                lastResourcePointer = resourcePointer;
                isNewSurface = true;
            }

            if (isNewSurface)
            {
                surfaceHeight = height;
                surfaceWidth = width;

                SurfaceChanged(resourcePointer, surfaceWidth, surfaceHeight);
            }

            if (RenderTargetView != null) return graphics.Render(RenderTargetView, projectionMatrix,
                surfaceWidth, surfaceHeight);

            throw new Exception("RenderTargetView is null");
        }

        public void UpdateViewPort(float angleOfViewDegrees, float nearClip, float farClip)
        {
            angleOfView = angleOfViewDegrees * (float)Math.PI / 180f;
            this.nearClip = nearClip;
            this.farClip = farClip;
            SetUpViewport(surfaceWidth, surfaceHeight);
        }

        public Tuple<int, int> GetPixelSize(Window window, Panel host)
        {
            double dpiScale = 1.0; // default value for 96 dpi

            // determine DPI
            // (as of .NET 4.6.1, this returns the DPI of the primary monitor, if you have several different DPIs)
            if (PresentationSource.FromVisual(window)?.CompositionTarget is HwndTarget hwndTarget)
            {
                dpiScale = hwndTarget.TransformToDevice.M11;
            }

            surfaceWidth = (int)(host.ActualWidth < 0 ? 0 : Math.Ceiling(host.ActualWidth * dpiScale));
            surfaceHeight = (int)(host.ActualHeight < 0 ? 0 : Math.Ceiling(host.ActualHeight * dpiScale));
            var pixelSize = new Tuple<int, int>(surfaceWidth, surfaceHeight);

            return pixelSize;
        }

        private Device InitializeD3D11Device()
        {
            Device device = null;

            // Initialize d3d11 device and deviceContext
            //
            try
            {
                const DeviceCreationFlags deviceFlags = DeviceCreationFlags.BgraSupport;

                FeatureLevel[] featureLevels = { FeatureLevel.Level_11_1 };
                int numFeatureLevels = featureLevels.Length;
                device = new Device(DriverType.Hardware, deviceFlags, featureLevels);
            }
            catch (Exception e)
            {
                throw new Exception("Could not create a Direct3D 10 or 11 device. " + e.Message);
            }

            if (device == null) throw new Exception("Cannot create D3D11 device");

            return device;
        }

        private void CreateRenderTargetView(IntPtr resourcePointer)
        {
            // Get a handle to the DirectX9 texture2D
            Resource d3dResource = (Resource)resourcePointer;
            IntPtr renderTextureHandle = d3dResource.QueryInterface<Resource>().SharedHandle;

            // use this handle to create a D3D11 Texture2D linked to the same resource.
            Resource d3d11Resource1 = device.OpenSharedResource<Resource>(renderTextureHandle);
            var texture2D = (Texture2D)d3d11Resource1.NativePointer;

            // Now, we can create the RenderTargetView
            RenderTargetViewDescription targetDescription = new()
            {
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Dimension = RenderTargetViewDimension.Texture2D
            };
            targetDescription.Texture2D.MipSlice = 0;

            RenderTargetView = new RenderTargetView(device, texture2D, targetDescription);

            // Set render target view to interOpImage
            deviceContext.OutputMerger.SetRenderTargets(renderTargetView: RenderTargetView);
        }

        private void SurfaceChanged(IntPtr resourcePointer, int surfWidth, int surfHeight)
        {
            CreateRenderTargetView(resourcePointer);
            SetUpViewport(surfWidth, surfHeight);
        }

        private void SetUpViewport(int width, int height)
        {
            surfaceWidth = width;
            surfaceHeight = height;
            RawViewportF vp;
            vp.Width = width;
            vp.Height = height;
            vp.MinDepth = 0;
            vp.MaxDepth = 1;
            vp.X = 0;
            vp.Y = 0;
            deviceContext.Rasterizer.SetViewport(viewport: vp);

            // Initialize the projection matrix
            projectionMatrix = RawMatrix.PerspectiveFovLH(angleOfView, width / (float)height, nearClip, farClip);
        }

    }
}
