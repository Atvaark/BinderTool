using System;

namespace BinderTool.Core.Dds.Enum
{
    [Flags]
    public enum DdsSurfaceFlags
    {
        /// <summary>
        ///     DDSCAPS_TEXTURE
        /// </summary>
        Texture = 0x00001000,

        /// <summary>
        ///     DDSCAPS_COMPLEX | DDSCAPS_MIPMAP
        /// </summary>
        MipMap = 0x00400008,

        /// <summary>
        ///     DDSCAPS_COMPLEX
        /// </summary>
        CubeMap = 0x00000008
    }
}
