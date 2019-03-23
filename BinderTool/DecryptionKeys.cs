using System;
using System.Collections.Generic;
using System.Text;

namespace BinderTool
{
    internal static class DecryptionKeys
    {
        private static readonly Dictionary<string, string> RsaKeyDictionary;

        private static readonly Dictionary<string, byte[]> AesKeyDictionary;

        static DecryptionKeys()
        {
            RsaKeyDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Data1.bhd", Data1Key },
                { "Data2.bhd", Data2Key },
                { "Data3.bhd", Data3Key },
                { "Data4.bhd", Data4Key },
                { "Data5.bhd", Data5Key },
                { "DLC1.bhd", Dlc1Key },
                { "DLC2.bhd", Dlc2Key },
            };

            AesKeyDictionary = new Dictionary<string, byte[]>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "regulation.regbnd.dcx.enc", RegulationFileKeyDs3 },
                { "enc_regulation.bnd.dcx", RegulationFileKeyDs2 },
            };
        }
        
        public static bool TryGetRsaFileKey(string file, out string key)
        {
            return RsaKeyDictionary.TryGetValue(file, out key);
        }

        public static bool TryGetAesFileKey(string file, out byte[] key)
        {
            return AesKeyDictionary.TryGetValue(file, out key);
        }

        /// <summary>
        /// The key can be read by setting a breakpoint on the "create_serialCipherKey" method of the SerialKeyGeneratorSPI class. 
        /// Signature: SerialCipherKey *__fastcall SerialKeyGeneratorSPI::create_serialCipherKey(
        ///     SerialKeyGeneratorSPI *this,
        ///     const void *pKeyType,
        ///     const void *pKey,
        ///     unsigned int keylen,
        ///     const void *pHeapAllocator)
        /// 
        /// These are the DarkSoulsIII.exe 1.3.1.0 offsets:
        /// Address (vtable): 0000000142AECBB8 (DLCR::DLSerialKeyGeneratorSPI::vftable + 0x08) 
        /// Address (method): 0000000141790180 
        /// </summary>
        public static readonly byte[] UserDataKeyDs3 =
        {
            0xFD, 0x46, 0x4D, 0x69, 0x5E, 0x69, 0xA3, 0x9A,
            0x10, 0xE3, 0x19, 0xA7, 0xAC, 0xE8, 0xB7, 0xFA
        };

        public static readonly byte[] UserDataKeyDs2 =
        {
            0xB7, 0xFD, 0x46, 0x3E, 0x4A, 0x9C, 0x11, 0x02,
            0xDF, 0x17, 0x39, 0xE5, 0xF3, 0xB2, 0xA5, 0x0F
        };
        
        /// <summary>
        /// <see cref="UserDataKeyDs3"/>
        /// </summary>
        public static readonly byte[] RegulationFileKeyDs3 = Encoding.ASCII.GetBytes("ds3#jn/8_7(rsY9pg55GFN7VFL#+3n/)");
        
        public static readonly byte[] RegulationFileKeyDs2 =
        {
            0x40, 0x17, 0x81, 0x30, 0xDF, 0x0A, 0x94, 0x54,
            0x33, 0x09, 0xE1, 0x71, 0xEC, 0xBF, 0x25, 0x4C
        };

        /// <summary>
        /// <see cref="UserDataKeyDs3"/>
        /// </summary>
        public static readonly byte[] NetworkSessionKeyDs3 = Encoding.ASCII.GetBytes("ds3vvhes09djxwcj");

        private const string Data1Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEA92l+AWx1aV7mzt+6r00bm/qnc4b6NH3VVr/v4UxMcfzushL8jsn9
ZSP1ss95ot/quk8dOJsp0+/bvxH+C9DEezzNLSqqAGd2jq2PYosj/6FhYAKjjMlK
jNxcVPsKQug0Zby+KYsENirmEXcmA1fzltrISf6d6LKB1UFHHN9NRkLCm3idE4Pu
9852kPHbiL14EqfDCDgwm7kLeQdt3kUbcmdhu/6dvP42HGxBmAYLNFD3iAe7qLML
MFzmKKHQD2fRQK/431Z3xPK6Jp245AdR0AwUYVvnXq+/97wMX0C6UKvAZ+b/1ytD
Nu8vZt++lhJ01SjTc2A4hVPz7g1EEO5/TQIEKkj5Jw==
-----END RSA PUBLIC KEY-----";

        private const string Data2Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBDAKCAQEAqhjoThWX8VwsTKTI1kjp0JBloCXhV8i99P1KPTCTDBnmhVQPdu+7
UQ5g4//eh0oqKaOUjet+0SP94QscjIIrhV91OzfIouIWgJJK/ROOP/A3sb5AlzPa
6YPcN8ODxR+esyrWhc6rHCt4qGvXVXrgh6zpZM5h5VCTSaup4qqIWm44EF3+FeYS
7faFg14rH0QEosieIIZFZmpI6SCJanlrVd+Zh13s4XcZfk0JdC2AEjxCQ2lKi3Un
WAMOcJc+8uHoMuNNo1PMpYQ6Z8Nzg5Cii7EnwbCDmuJw58tFBmbOVHZpkY93VIeF
maJXSE7ztTp0qTa05YZUsiU3g9HplkeTUwIFAP/xKZE=
-----END RSA PUBLIC KEY-----";

        private const string Data3Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBDAKCAQEAx5jlgIvoHQLwSFsAwKFZbNo3fgZ89C7tj4hwiZsQVg8QnNZohXl5
S5Ep9pS2biOFsSkuZMXKmfYErh2CsdFbr7QR7kvPPianXNrkCI4xlfQwJvMmkLm9
6/JmRIUzTWp0kKJUJZJH/UIrXNn7fmk8Vmx1bQIi8bumGSl3gxeMhutv/lC9khsY
Tn0ABTJAbIbwNZ5GPXxzQZuQPXXDY52Gm+Fx7Yy1LiK/B6isIDJUN0xdgxdaXxGN
f5pPocMJjng0Ob3cjhGvdkysll/jYFnRx0La3CGmtLcXMtHheEQxzGueGDa/lkkl
AvvEXtcpKfyFQWcUheQZ8LngAh/UTJHtQwIFAOpVoU8=
-----END RSA PUBLIC KEY-----";

        private const string Data4Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAq8RyArk+eqMAcxLAHUDRYV7yScNKZpKSxGmgJZQ7y6Y8f5wdrNCt
byXfmsdQECStIGlkwWjtfm8t/bRZuxxPciAYaFsWo0Ze2BB6uY6ZteNpLJn82qbL
TXATf+af3kSrvICfvJwRzbfA/PRJRkHj2gJ6Tc7g6HK7S/4TiCZirq+c/zLY3gb8
A8uIFNI4j0qxTzfoAlS7K6spZjfnhZ6l7pYFh+glz15wAbppC9Oy/u5vUacozf4v
nacbUHD47ds9EZPZDHk3LfJbioHwtUzJfyBqZmIpI33yiwImPpb96zwvQU86TaXK
sJrTmSs/48BeDsQwXuaqOg+6noETBx3pgQIEGM2Ohw==
-----END RSA PUBLIC KEY-----";

        private const string Data5Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBDAKCAQEAu75/UbXwHdvu/p49TwnY7Ou6DAuZYFAtLUkw/R4nvm0HWVlRsZiB
LG3MOG6sPmK2Zc3JLBU2QK4uKazZ9VrmotM4OpYr03q2tiFnv3NfCvB1UeIJIKe3
kVhHNZIbvrwEP9a5UCnrSHD+u+Fj5MQBr4yrEitwrNVvIC4J0Ez1Ppn3+D8ff8Xg
QRP9qCVLI3X/wdQDea+B5o8PWaYEL9MKnnL1Tq4h+4PRYHcQR8/GXBTrc3x9q3cP
QRDWHbRYhIfWSP9urtagjcsmcuG+p34fp+KyWOwkil3FJqwH1KgSTbk9Tb0oBPzq
TCJKeE/wgu6hY++lBi5T3ArHZZcsbXzV6wIFAPlRTMc=
-----END RSA PUBLIC KEY-----";

        private const string Dlc1Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAsCGM9dFwzaIOUIin3DXy7xrmI2otKGLZJQyKi5X3znKhSTywpcFc
KoW6hgjeh4fJW24jhzwBosG6eAzDINm+K02pHCG8qZ/D/hIbu+ui0ENDKqrVyFhn
QtX5/QJkVQtj8M4a0FIfdtE3wkxaKtP6IXWIy4DesSdGWONVWLfi2eq62A5ts5MF
qMoSV3XjTYuCgXqZQ6eOE+NIBQRqpZxLNFSzbJwWXpAg2kBMkpy5+ywOByjmWzUw
jnIFl1T17R8DpTU/93ojx+/q1p+b1o5is5KcoP7QwjOqzjHJH8bTytzRbgmRcDMW
3ahxgI070d45TMXK2YwRzI6/JbM1P29anQIEFezyYw==
-----END RSA PUBLIC KEY-----";

        private const string Dlc2Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAtCXU9a/GBMVoqtpQox9p0/5sWPaIvDp8avLFnIBhN7vkgTwulZHi
u64vZAiUAdVeFX4F+Qtk+5ivK488Mu2CzAMJcz5RvyMQJtOQXuDDqzIv21Tr5zuu
sswoErHxxP8TZNxkHm7Ram7Oqtn7LQnMTYxsBgZZ34yJkRtAmZnGoCu5YaUR5euk
8lF75idi97ssczUNV212tLzIMa1YOV7sxOb7+gc0VTIqs3pa+OXLPI/bMfwUc/KN
jur5aLDDntQHGx5zuNtc78gMGwlmPqDhgTusKPO4VyKvoL0kITYvukoXJATaa1HI
WVUjhLm+/uj8r8PNgolerDeS+8FM5Bpe9QIEHwCZLw==
-----END RSA PUBLIC KEY-----";
    }
}
