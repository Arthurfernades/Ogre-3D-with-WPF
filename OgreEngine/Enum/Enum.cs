namespace OgreEngine
{
    public partial class OgreImage
    {
        public enum TextureUsage : int
        {
            TU_AUTOMIPMAP = 0x10,

            /// <summary>
            /// ** This texture will be a render target, i.e. used as a target for render to texture setting 
            /// this flag will ignore all other texture usages except TU_AUTOMIPMAP, TU_UNORDERED_ACCESS, TU_NOT_SAMPLED 
            /// </summary>
            TU_RENDERTARGET = 0x20,

            TU_NOT_SAMPLED = 0x40,

            TU_UNORDERED_ACCESS = 0x80,
        }


    }
}
