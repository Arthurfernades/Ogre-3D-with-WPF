using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;
using DeviceContext = SharpDX.Direct3D11.DeviceContext;

namespace WpfSharpDXLib
{
    public interface IGraphics
    {
        bool Initialize(Device device, DeviceContext deviceContext);
        bool Render(RenderTargetView renderTargetView, RawMatrix projectionMatrix, int surfaceWidth, int surfaceHeight);
    }
}
