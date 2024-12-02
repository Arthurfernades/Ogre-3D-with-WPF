namespace OgreEngine
{
    public partial class OgreImage
    {
        public enum TextureUsage : int
        {
            TU_AUTOMIPMAP = 0x10,

            TU_RENDERTARGET = 0x20,

            TU_NOT_SAMPLED = 0x40,

            TU_UNORDERED_ACCESS = 0x80,

            TU_SHARED_RESOURCE = 0x100
        }
    }
}
