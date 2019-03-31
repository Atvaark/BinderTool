using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Text;
using BinderTool.Core;

namespace BinderTool
{
    internal static class DecryptionKeys
    {
        private static readonly Dictionary<string, string> Ds3RsaKeyDictionary;

        private static readonly Dictionary<string, string> SekiroRsaKeyDictionary;

        private static readonly Dictionary<string, byte[]> Ds3AesKeyDictionary;

        static DecryptionKeys()
        {
            Ds3RsaKeyDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Data1.bhd", Ds3Data1Key },
                { "Data2.bhd", Ds3Data2Key },
                { "Data3.bhd", Ds3Data3Key },
                { "Data4.bhd", Ds3Data4Key },
                { "Data5.bhd", Ds3Data5Key },
                { "DLC1.bhd", Ds3Dlc1Key },
                { "DLC2.bhd", Ds3Dlc2Key },
            };

            SekiroRsaKeyDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Data1.bhd", SekiroData1Key },
                { "Data2.bhd", SekiroData2Key },
                { "Data3.bhd", SekiroData3Key },
                { "Data4.bhd", SekiroData4Key },
                { "Data5.bhd", SekiroData5Key },
                { "Data.bhd", SekiroArtworkKey }
            };

            Ds3AesKeyDictionary = new Dictionary<string, byte[]>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "regulation.regbnd.dcx.enc", RegulationFileKeyDs3 },
                { "enc_regulation.bnd.dcx", RegulationFileKeyDs2 },
            };
        }
        
        public static bool TryGetRsaFileKey(GameVersion version, string file, out string key)
        {
            switch (version)
            {
                case GameVersion.DarkSouls3:
                    return Ds3RsaKeyDictionary.TryGetValue(file, out key);
                case GameVersion.Sekiro:
                    return SekiroRsaKeyDictionary.TryGetValue(file, out key);
            }

            key = null;
            return false;
        }

        public static bool TryGetAesFileKey(string file, out byte[] key)
        {
            return Ds3AesKeyDictionary.TryGetValue(file, out key);
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

        private const string Ds3Data1Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEA05hqyboW/qZaJ3GBIABFVt1X1aa0/sKINklvpkTRC+5Ytbxvp18L
M1gN6gjTgSJiPUgdlaMbptVa66MzvilEk60aHyVVEhtFWy+HzUZ3xRQm6r/2qsK3
8wXndgEU5JIT2jrBXZcZfYDCkUkjsGVkYqjBNKfp+c5jlnNwbieUihWTSEO+DA8n
aaCCzZD3e7rKhDQyLCkpdsGmuqBvl02Ou7QeehbPPno78mOYs2XkP6NGqbFFGQwa
swyyyXlQ23N15ZaFGRRR0xYjrX4LSe6OJ8Mx/Zkec0o7L28CgwCTmcD2wO8TEATE
AUbbV+1Su9uq2+wQxgnsAp+xzhn9og9hmwIEC35bSQ==
-----END RSA PUBLIC KEY-----";

        private const string Ds3Data2Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAvCZAK9UfPdk5JaTlG7n1r0LSVzIan3h0BSLaMXQHOwO7tTGpvtdX
m2ZLY9y8SVmOxWTQqRq14aVGLTKDyH87hPuKd47Y0E5K5erTqBbXW6AD4El1eir2
VJz/pwHt73FVziOlAnao1A5MsAylZ9B5QJyzHJQG+LxzMzmWScyeXlQLOKudfiIG
0qFw/xhRMLNAI+iypkzO5NKblYIySUV5Dx7649XdsZ5UIwJUhxONsKuGS+MbeTFB
mTMehtNj5EwPxGdT4CBPAWdeyPhpoHJHCbgrtnN9akwQmpwdBBxT/sTD16Adn9B+
TxuGDQQALed4S4KvM+fadx27pQz8pP9VLwIEL67iCQ==
-----END RSA PUBLIC KEY-----";

        private const string Ds3Data3Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAqLytWD20TSXPeAA1RGDwPW18nJwe2rBX+0HPtdzFmQc/KmQlWrP+
94k6KClK5f7m0xUHwT8+yFGLxPdRvUPyOhBEnRA6tkObVDSxij5y0Jh4h4ilAO73
I8VMcmscS71UKkck4444+eR4vVd+SPlzIu8VgqLefvEn/sX/pAevDp7w+gD0NgvO
e9U6iWEXKwTOPB97X+Y2uB03gSSognmV8h2dtUFJ4Ryn5jrpWmsuUbdvGp0CWBKH
CFruNXnfsG0hlf9LqbVmEzbFl/MhjBmbVjjtelorZsoLPK+OiPTHW5EcwwnPh1vH
FFGM7qRMc0yvHqJnniEWDsSz8Bvg+GxpgQIEC8XNVw==
-----END RSA PUBLIC KEY-----";

        private const string Ds3Data4Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEArfUaZWjYAUaZ0q+5znpX55GeyepawCZ5NnsMjIW9CA3vrOgUGRkh
6aAU9frlafQ81LQMRgAznOnQGE7K3ChfySDpq6b47SKm4bWPqd7Ulh2DTxIgi6QP
qm4UUJL2dkLaCnuoya/pGMOOvhT1LD/0CKo/iKwfBcYf/OAnwSnxMRC3SNRugyvF
ylCet9DEdL5L8uBEa4sV4U288ZxZSZLg2tB10xy5SHAsm1VNP4Eqw5iJbqHEDKZW
n2LJP5t5wpEJvV2ACiA4U5fyjQLDzRwtCKzeK7yFkKiZI95JJhU/3DnVvssjIxku
gYZkS9D3k9m+tkNe0VVrd4mBEmqVxg+V9wIEL6Y6tw==
-----END RSA PUBLIC KEY-----";

        private const string Ds3Data5Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAvKTlU3nka4nQesRnYg1NWovCCTLhEBAnjmXwI69lFYfc4lvZsTrQ
E0Y25PtoP0ZddA3nzflJNz1rBwAkqfBRGTeeTCAyoNp/iel3EAkid/pKOt3JEkHx
rojRuWYSQ0EQawcBbzCfdLEjizmREepRKHIUSDWgu0HTmwSFHHeCFbpBA1h99L2X
izH5XFTOu0UIcUmBLsK6DYsIj5QGrWaxwwXcTJN/X+/syJ/TbQK9W/TCGaGiirGM
1u2wvZXSZ7uVM3CHwgNhAMiqLvqORygcDeNqxgq+dXDTxka43j7iPJWdHs8b25fy
aH3kbUxKlDGaEENNNyZQcQrgz8Q76jIE0QIEFUsz9w==
-----END RSA PUBLIC KEY-----";

        private const string Ds3Dlc1Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAsCGM9dFwzaIOUIin3DXy7xrmI2otKGLZJQyKi5X3znKhSTywpcFc
KoW6hgjeh4fJW24jhzwBosG6eAzDINm+K02pHCG8qZ/D/hIbu+ui0ENDKqrVyFhn
QtX5/QJkVQtj8M4a0FIfdtE3wkxaKtP6IXWIy4DesSdGWONVWLfi2eq62A5ts5MF
qMoSV3XjTYuCgXqZQ6eOE+NIBQRqpZxLNFSzbJwWXpAg2kBMkpy5+ywOByjmWzUw
jnIFl1T17R8DpTU/93ojx+/q1p+b1o5is5KcoP7QwjOqzjHJH8bTytzRbgmRcDMW
3ahxgI070d45TMXK2YwRzI6/JbM1P29anQIEFezyYw==
-----END RSA PUBLIC KEY-----";

        private const string Ds3Dlc2Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAtCXU9a/GBMVoqtpQox9p0/5sWPaIvDp8avLFnIBhN7vkgTwulZHi
u64vZAiUAdVeFX4F+Qtk+5ivK488Mu2CzAMJcz5RvyMQJtOQXuDDqzIv21Tr5zuu
sswoErHxxP8TZNxkHm7Ram7Oqtn7LQnMTYxsBgZZ34yJkRtAmZnGoCu5YaUR5euk
8lF75idi97ssczUNV212tLzIMa1YOV7sxOb7+gc0VTIqs3pa+OXLPI/bMfwUc/KN
jur5aLDDntQHGx5zuNtc78gMGwlmPqDhgTusKPO4VyKvoL0kITYvukoXJATaa1HI
WVUjhLm+/uj8r8PNgolerDeS+8FM5Bpe9QIEHwCZLw==
-----END RSA PUBLIC KEY-----";

        private const string SekiroData1Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEA92l+AWx1aV7mzt+6r00bm/qnc4b6NH3VVr/v4UxMcfzushL8jsn9
ZSP1ss95ot/quk8dOJsp0+/bvxH+C9DEezzNLSqqAGd2jq2PYosj/6FhYAKjjMlK
jNxcVPsKQug0Zby+KYsENirmEXcmA1fzltrISf6d6LKB1UFHHN9NRkLCm3idE4Pu
9852kPHbiL14EqfDCDgwm7kLeQdt3kUbcmdhu/6dvP42HGxBmAYLNFD3iAe7qLML
MFzmKKHQD2fRQK/431Z3xPK6Jp245AdR0AwUYVvnXq+/97wMX0C6UKvAZ+b/1ytD
Nu8vZt++lhJ01SjTc2A4hVPz7g1EEO5/TQIEKkj5Jw==
-----END RSA PUBLIC KEY-----";

        private const string SekiroData2Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBDAKCAQEAqhjoThWX8VwsTKTI1kjp0JBloCXhV8i99P1KPTCTDBnmhVQPdu+7
UQ5g4//eh0oqKaOUjet+0SP94QscjIIrhV91OzfIouIWgJJK/ROOP/A3sb5AlzPa
6YPcN8ODxR+esyrWhc6rHCt4qGvXVXrgh6zpZM5h5VCTSaup4qqIWm44EF3+FeYS
7faFg14rH0QEosieIIZFZmpI6SCJanlrVd+Zh13s4XcZfk0JdC2AEjxCQ2lKi3Un
WAMOcJc+8uHoMuNNo1PMpYQ6Z8Nzg5Cii7EnwbCDmuJw58tFBmbOVHZpkY93VIeF
maJXSE7ztTp0qTa05YZUsiU3g9HplkeTUwIFAP/xKZE=
-----END RSA PUBLIC KEY-----";

        private const string SekiroData3Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBDAKCAQEAx5jlgIvoHQLwSFsAwKFZbNo3fgZ89C7tj4hwiZsQVg8QnNZohXl5
S5Ep9pS2biOFsSkuZMXKmfYErh2CsdFbr7QR7kvPPianXNrkCI4xlfQwJvMmkLm9
6/JmRIUzTWp0kKJUJZJH/UIrXNn7fmk8Vmx1bQIi8bumGSl3gxeMhutv/lC9khsY
Tn0ABTJAbIbwNZ5GPXxzQZuQPXXDY52Gm+Fx7Yy1LiK/B6isIDJUN0xdgxdaXxGN
f5pPocMJjng0Ob3cjhGvdkysll/jYFnRx0La3CGmtLcXMtHheEQxzGueGDa/lkkl
AvvEXtcpKfyFQWcUheQZ8LngAh/UTJHtQwIFAOpVoU8=
-----END RSA PUBLIC KEY-----";

        private const string SekiroData4Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAq8RyArk+eqMAcxLAHUDRYV7yScNKZpKSxGmgJZQ7y6Y8f5wdrNCt
byXfmsdQECStIGlkwWjtfm8t/bRZuxxPciAYaFsWo0Ze2BB6uY6ZteNpLJn82qbL
TXATf+af3kSrvICfvJwRzbfA/PRJRkHj2gJ6Tc7g6HK7S/4TiCZirq+c/zLY3gb8
A8uIFNI4j0qxTzfoAlS7K6spZjfnhZ6l7pYFh+glz15wAbppC9Oy/u5vUacozf4v
nacbUHD47ds9EZPZDHk3LfJbioHwtUzJfyBqZmIpI33yiwImPpb96zwvQU86TaXK
sJrTmSs/48BeDsQwXuaqOg+6noETBx3pgQIEGM2Ohw==
-----END RSA PUBLIC KEY-----";

        private const string SekiroData5Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBDAKCAQEAu75/UbXwHdvu/p49TwnY7Ou6DAuZYFAtLUkw/R4nvm0HWVlRsZiB
LG3MOG6sPmK2Zc3JLBU2QK4uKazZ9VrmotM4OpYr03q2tiFnv3NfCvB1UeIJIKe3
kVhHNZIbvrwEP9a5UCnrSHD+u+Fj5MQBr4yrEitwrNVvIC4J0Ez1Ppn3+D8ff8Xg
QRP9qCVLI3X/wdQDea+B5o8PWaYEL9MKnnL1Tq4h+4PRYHcQR8/GXBTrc3x9q3cP
QRDWHbRYhIfWSP9urtagjcsmcuG+p34fp+KyWOwkil3FJqwH1KgSTbk9Tb0oBPzq
TCJKeE/wgu6hY++lBi5T3ArHZZcsbXzV6wIFAPlRTMc=
-----END RSA PUBLIC KEY-----";

        /// <summary>
        ///     Same as <see cref="Ds3Dlc1Key"/>.
        /// </summary>
        private const string SekiroUnknownKey1 =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAsCGM9dFwzaIOUIin3DXy7xrmI2otKGLZJQyKi5X3znKhSTywpcFc
KoW6hgjeh4fJW24jhzwBosG6eAzDINm+K02pHCG8qZ/D/hIbu+ui0ENDKqrVyFhn
QtX5/QJkVQtj8M4a0FIfdtE3wkxaKtP6IXWIy4DesSdGWONVWLfi2eq62A5ts5MF
qMoSV3XjTYuCgXqZQ6eOE+NIBQRqpZxLNFSzbJwWXpAg2kBMkpy5+ywOByjmWzUw
jnIFl1T17R8DpTU/93ojx+/q1p+b1o5is5KcoP7QwjOqzjHJH8bTytzRbgmRcDMW
3ahxgI070d45TMXK2YwRzI6/JbM1P29anQIEFezyYw==
-----END RSA PUBLIC KEY-----";

        /// <summary>
        ///     Same as <see cref="Ds3Dlc2Key"/>.
        /// </summary>
        private const string SekiroUnknownKey2 =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAtCXU9a/GBMVoqtpQox9p0/5sWPaIvDp8avLFnIBhN7vkgTwulZHi
u64vZAiUAdVeFX4F+Qtk+5ivK488Mu2CzAMJcz5RvyMQJtOQXuDDqzIv21Tr5zuu
sswoErHxxP8TZNxkHm7Ram7Oqtn7LQnMTYxsBgZZ34yJkRtAmZnGoCu5YaUR5euk
8lF75idi97ssczUNV212tLzIMa1YOV7sxOb7+gc0VTIqs3pa+OXLPI/bMfwUc/KN
jur5aLDDntQHGx5zuNtc78gMGwlmPqDhgTusKPO4VyKvoL0kITYvukoXJATaa1HI
WVUjhLm+/uj8r8PNgolerDeS+8FM5Bpe9QIEHwCZLw==
-----END RSA PUBLIC KEY-----";

        private const string SekiroArtworkKey =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBDAKCAQEAxFOPK7c3E2Tu8HSS3NUlWUHdlJZIJiHf/0DhyLZUP68iEhJ8SLQ0
sDAgFBxIAEZQcVZBKLhTZiTyqaqIolgCDH6ZcMlWOGOWj3G6PuhPeb/7ZeQSo7Xv
plGCovqnioRoaFf4gVZDsbpVXIGNXwWsL5kArQiQo3ZrMs17/t77yZ6avC/1hnFp
ks1k3uQ269NKZOpU6Q73I8yolUFGJFBlm9uHqRfZC0wcA+IXjo96C1PoTKJQktkh
J07MPeoeckkAGdUv9S+kcDN04SAGMJJBWB9OOvn2Qle938gmCY6beeuk8c/l67zs
ChgwGmsLdVr7W6hZL3aNvsf/BWFQ+e7+tQIFANZbM50=
-----END RSA PUBLIC KEY-----";
    }
}
