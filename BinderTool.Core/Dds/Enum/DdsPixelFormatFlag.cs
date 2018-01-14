using System;

namespace BinderTool.Core.Dds.Enum
{
    [Flags]
    public enum DdsPixelFormatFlag : uint
    {
        /// <summary>
        ///     DDPF_ALPHA
        /// </summary>
        Alpha = 0x00000002,

        /// <summary>
        ///     DDPF_FOURCC
        /// </summary>
        FourCc = 0x00000004,

        /// <summary>
        ///     DDPF_RGB
        /// </summary>
        Rgb = 0x00000040,

        /// <summary>
        ///     DDPF_RGB | DDPF_ALPHAPIXELS
        /// </summary>
        Rgba = 0x00000041,

        /// <summary>
        ///     DDPF_LUMINANCE
        /// </summary>
        Luminance = 0x00020000,

        /// <summary>
        ///     Nvidia custom DDPF_NORMA
        /// </summary>
        Normal = 0x80000000
    }
}
