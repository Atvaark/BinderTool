using System;

namespace BinderTool.Core.Dds.Enum
{
    [Flags]
    public enum DdsFileHeaderFlags
    {
        /// <summary>
        ///     DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT
        /// </summary>
        Texture = 0x00001007,

        /// <summary>
        ///     DDSD_MIPMAPCOUNT
        /// </summary>
        MipMap = 0x00020000,

        /// <summary>
        ///     DDSD_DEPTH
        /// </summary>
        Volume = 0x00800000,

        /// <summary>
        ///     DDSD_PITCH
        /// </summary>
        Pitch = 0x00000008,

        /// <summary>
        ///     DDSD_LINEARSIZE
        /// </summary>
        LinearSize = 0x00080000
    }
}
