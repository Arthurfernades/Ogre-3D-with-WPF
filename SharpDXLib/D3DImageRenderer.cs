using SharpDX.Direct3D11;
using System;
using System.Windows.Interop;

namespace SharpDXLib
{

    public class D3DImageRenderer : D3DImage
    {
        private Texture2D _sharedTexture;
        private Device _device;

        public void Initialize(IntPtr sharedHandle)
        {
            // Inicializar o dispositivo DirectX
            _device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport);

            // Vincular a textura compartilhada
            _sharedTexture = _device.OpenSharedResource<Texture2D>(sharedHandle);

            Lock();
            SetBackBuffer(D3DResourceType.IDirect3DSurface9, _sharedTexture.NativePointer);
            Unlock();
        }

        public void UpdateFrame()
        {
            Lock();
            AddDirtyRect(new System.Windows.Int32Rect(0, 0, PixelWidth, PixelHeight));
            Unlock();
        }

        //protected override void Dispose(bool disposing)
        //{
        //    base.Dispose(disposing);
        //    _sharedTexture?.Dispose();
        //    _device?.Dispose();
        //}
    }
}