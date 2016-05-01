using System.Collections.Generic;

namespace BinderTool
{
    internal static class DecryptionKeys
    {
        private static readonly Dictionary<string, string> FileKeyDictionary;

        static DecryptionKeys()
        {
            FileKeyDictionary = new Dictionary<string, string>
            {
                { "Data1.bhd", Data1Key },
                { "Data2.bhd", Data2Key },
                { "Data3.bhd", Data3Key },
                { "Data4.bhd", Data4Key },
                { "Data5.bhd", Data5Key }
            };
        }

        public static string GetFileKey(string file)
        {
            return FileKeyDictionary[file];
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
        public static readonly byte[] UserDataKey =
        {
            0xFD, 0x46, 0x4D, 0x69, 0x5E, 0x69, 0xA3, 0x9A, 0x10, 0xE3, 0x19, 0xA7, 0xAC, 0xE8, 0xB7, 0xFA
        };

        /// <summary>
        /// <see cref="DecryptionKeys.UserDataKey"/>
        /// </summary>
        public static readonly byte[] RegulationFileKey =
        {
            0x64, 0x73, 0x33, 0x23, 0x6A, 0x6E, 0x2F, 0x38, 0x5F, 0x37, 0x28, 0x72, 0x73, 0x59, 0x39, 0x70,
            0x67, 0x35, 0x35, 0x47, 0x46, 0x4E, 0x37, 0x56, 0x46, 0x4C, 0x23, 0x2B, 0x33, 0x6E, 0x2F, 0x29
        };

        /// <summary>
        /// <see cref="DecryptionKeys.UserDataKey"/>
        /// </summary>
        public static readonly byte[] NetworkSessionKey =
        {
            0x64, 0x73, 0x33, 0x76, 0x76, 0x68, 0x65, 0x73, 0x30, 0x39, 0x64, 0x6A, 0x78, 0x77, 0x63, 0x6A
        };

        private const string Data1Key =
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEA05hqyboW/qZaJ3GBIABFVt1X1aa0/sKINklvpkTRC+5Ytbxvp18L
M1gN6gjTgSJiPUgdlaMbptVa66MzvilEk60aHyVVEhtFWy+HzUZ3xRQm6r/2qsK3
8wXndgEU5JIT2jrBXZcZfYDCkUkjsGVkYqjBNKfp+c5jlnNwbieUihWTSEO+DA8n
aaCCzZD3e7rKhDQyLCkpdsGmuqBvl02Ou7QeehbPPno78mOYs2XkP6NGqbFFGQwa
swyyyXlQ23N15ZaFGRRR0xYjrX4LSe6OJ8Mx/Zkec0o7L28CgwCTmcD2wO8TEATE
AUbbV+1Su9uq2+wQxgnsAp+xzhn9og9hmwIEC35bSQ==
-----END RSA PUBLIC KEY-----";

        private const string Data2Key = 
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAvCZAK9UfPdk5JaTlG7n1r0LSVzIan3h0BSLaMXQHOwO7tTGpvtdX
m2ZLY9y8SVmOxWTQqRq14aVGLTKDyH87hPuKd47Y0E5K5erTqBbXW6AD4El1eir2
VJz/pwHt73FVziOlAnao1A5MsAylZ9B5QJyzHJQG+LxzMzmWScyeXlQLOKudfiIG
0qFw/xhRMLNAI+iypkzO5NKblYIySUV5Dx7649XdsZ5UIwJUhxONsKuGS+MbeTFB
mTMehtNj5EwPxGdT4CBPAWdeyPhpoHJHCbgrtnN9akwQmpwdBBxT/sTD16Adn9B+
TxuGDQQALed4S4KvM+fadx27pQz8pP9VLwIEL67iCQ==
-----END RSA PUBLIC KEY-----";

        private const string Data3Key = 
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAqLytWD20TSXPeAA1RGDwPW18nJwe2rBX+0HPtdzFmQc/KmQlWrP+
94k6KClK5f7m0xUHwT8+yFGLxPdRvUPyOhBEnRA6tkObVDSxij5y0Jh4h4ilAO73
I8VMcmscS71UKkck4444+eR4vVd+SPlzIu8VgqLefvEn/sX/pAevDp7w+gD0NgvO
e9U6iWEXKwTOPB97X+Y2uB03gSSognmV8h2dtUFJ4Ryn5jrpWmsuUbdvGp0CWBKH
CFruNXnfsG0hlf9LqbVmEzbFl/MhjBmbVjjtelorZsoLPK+OiPTHW5EcwwnPh1vH
FFGM7qRMc0yvHqJnniEWDsSz8Bvg+GxpgQIEC8XNVw==
-----END RSA PUBLIC KEY-----";

        private const string Data4Key = 
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEArfUaZWjYAUaZ0q+5znpX55GeyepawCZ5NnsMjIW9CA3vrOgUGRkh
6aAU9frlafQ81LQMRgAznOnQGE7K3ChfySDpq6b47SKm4bWPqd7Ulh2DTxIgi6QP
qm4UUJL2dkLaCnuoya/pGMOOvhT1LD/0CKo/iKwfBcYf/OAnwSnxMRC3SNRugyvF
ylCet9DEdL5L8uBEa4sV4U288ZxZSZLg2tB10xy5SHAsm1VNP4Eqw5iJbqHEDKZW
n2LJP5t5wpEJvV2ACiA4U5fyjQLDzRwtCKzeK7yFkKiZI95JJhU/3DnVvssjIxku
gYZkS9D3k9m+tkNe0VVrd4mBEmqVxg+V9wIEL6Y6tw==
-----END RSA PUBLIC KEY-----";

        private const string Data5Key = 
@"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAvKTlU3nka4nQesRnYg1NWovCCTLhEBAnjmXwI69lFYfc4lvZsTrQ
E0Y25PtoP0ZddA3nzflJNz1rBwAkqfBRGTeeTCAyoNp/iel3EAkid/pKOt3JEkHx
rojRuWYSQ0EQawcBbzCfdLEjizmREepRKHIUSDWgu0HTmwSFHHeCFbpBA1h99L2X
izH5XFTOu0UIcUmBLsK6DYsIj5QGrWaxwwXcTJN/X+/syJ/TbQK9W/TCGaGiirGM
1u2wvZXSZ7uVM3CHwgNhAMiqLvqORygcDeNqxgq+dXDTxka43j7iPJWdHs8b25fy
aH3kbUxKlDGaEENNNyZQcQrgz8Q76jIE0QIEFUsz9w==
-----END RSA PUBLIC KEY-----";
    }
}
