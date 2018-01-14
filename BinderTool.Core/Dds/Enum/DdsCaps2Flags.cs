using System;

namespace BinderTool.Core.Dds.Enum
{
    [Flags]
    public enum DdsCaps2Flags
    {
        /// <summary>
        ///     DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEX
        /// </summary>
        PositiveX = 0x00000600,

        /// <summary>
        ///     DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEX
        /// </summary>
        NegativeX = 0x00000a00,

        /// <summary>
        ///     DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEY
        /// </summary>
        PositiveY = 0x00001200,

        /// <summary>
        ///     DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEY
        /// </summary>
        NegativeY = 0x00002200,

        /// <summary>
        ///     DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEZ
        /// </summary>
        PositiveZ = 0x00004200,

        /// <summary>
        ///     DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEZ
        /// </summary>
        NegativeZ = 0x00008200,

        /// <summary>
        ///     DDSCAPS2_CUBEMAP |
        ///     DDSCAPS2_CUBEMAP_POSITIVEX | DDSCAPS2_CUBEMAP_NEGATIVEX |
        ///     DDSCAPS2_CUBEMAP_POSITIVEY | DDSCAPS2_CUBEMAP_POSITIVEY |
        ///     DDSCAPS2_CUBEMAP_POSITIVEZ | DDSCAPS2_CUBEMAP_NEGATIVEZ
        /// </summary>
        AllFaces = PositiveX | NegativeX | PositiveY | NegativeY | PositiveZ | NegativeZ,

        /// <summary>
        ///     DDSCAPS2_VOLUME
        /// </summary>
        Volume = 0x00200000
    }
}
