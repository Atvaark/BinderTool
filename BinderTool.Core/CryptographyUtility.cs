using System;
using System.IO;
using BinderTool.Core.Bhd5;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;

namespace BinderTool.Core
{
    public static class CryptographyUtility
    {
        public static MemoryStream DecryptAesEcb(Stream inputStream, byte[] key)
        {
            var cipher = CreateAesEcbCipher(key);
            return DecryptAes(inputStream, cipher, inputStream.Length);
        }

        public static MemoryStream DecryptAesCbc(Stream inputStream, byte[] key, byte[] iv)
        {
            AesEngine engine = new AesEngine();
            KeyParameter keyParameter = new KeyParameter(key);
            ICipherParameters parameters = new ParametersWithIV(keyParameter, iv);

            BufferedBlockCipher cipher = new BufferedBlockCipher(new CbcBlockCipher(engine));
            cipher.Init(false, parameters);
            return DecryptAes(inputStream, cipher, inputStream.Length);
        }

        public static MemoryStream DecryptAesCtr(Stream inputStream, byte[] key, byte[] iv)
        {
            AesEngine engine = new AesEngine();
            KeyParameter keyParameter = new KeyParameter(key);
            ICipherParameters parameters = new ParametersWithIV(keyParameter, iv);

            BufferedBlockCipher cipher = new BufferedBlockCipher(new SicBlockCipher(engine));
            cipher.Init(false, parameters);
            return DecryptAes(inputStream, cipher, inputStream.Length);
        }

        private static BufferedBlockCipher CreateAesEcbCipher(byte[] key)
        {
            AesEngine engine = new AesEngine();
            KeyParameter parameter = new KeyParameter(key);
            BufferedBlockCipher cipher = new BufferedBlockCipher(engine);
            cipher.Init(false, parameter);
            return cipher;
        }

        private static MemoryStream DecryptAes(Stream inputStream, BufferedBlockCipher cipher, long length)
        {
            int blockSize = cipher.GetBlockSize();
            long inputLength = inputStream.Length;
            if (inputLength % blockSize > 0)
            {
                inputLength += blockSize - inputLength % blockSize;
            }

            byte[] input = new byte[inputLength];
            byte[] output = new byte[cipher.GetOutputSize((int)inputLength)];

            inputStream.Read(input, 0, (int)length);

            int len = cipher.ProcessBytes(input, 0, input.Length, output, 0);
            cipher.DoFinal(output, len);

            MemoryStream outputStream = new MemoryStream();
            outputStream.Write(output, 0, input.Length);
            outputStream.Seek(0, SeekOrigin.Begin);
            return outputStream;
        }

        /// <summary>
        ///     Decrypts a file with a provided decryption key.
        /// </summary>
        /// <param name="filePath">An encrypted file</param>
        /// <param name="key">The RSA key in PEM format</param>
        /// <exception cref="ArgumentNullException">When the argument filePath is null</exception>
        /// <exception cref="ArgumentNullException">When the argument keyPath is null</exception>
        /// <returns>A memory stream with the decrypted file</returns>
        public static MemoryStream DecryptRsa(string filePath, string key)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            AsymmetricKeyParameter keyParameter = GetKeyOrDefault(key);
            RsaEngine engine = new RsaEngine();
            engine.Init(false, keyParameter);

            MemoryStream outputStream = new MemoryStream();
            using (FileStream inputStream = File.OpenRead(filePath))
            {

                int inputBlockSize = engine.GetInputBlockSize();
                int outputBlockSize = engine.GetOutputBlockSize();
                byte[] inputBlock = new byte[inputBlockSize];
                while (inputStream.Read(inputBlock, 0, inputBlock.Length) > 0)
                {
                    byte[] outputBlock = engine.ProcessBlock(inputBlock, 0, inputBlockSize);

                    int requiredPadding = outputBlockSize - outputBlock.Length;
                    if (requiredPadding > 0)
                    {
                        byte[] paddedOutputBlock = new byte[outputBlockSize];
                        outputBlock.CopyTo(paddedOutputBlock, requiredPadding);
                        outputBlock = paddedOutputBlock;
                    }

                    outputStream.Write(outputBlock, 0, outputBlock.Length);
                }
            }

            outputStream.Seek(0, SeekOrigin.Begin);
            return outputStream;
        }

        public static AsymmetricKeyParameter GetKeyOrDefault(string key)
        {
            try
            {
                PemReader pemReader = new PemReader(new StringReader(key));
                return (AsymmetricKeyParameter)pemReader.ReadObject();
            }
            catch
            {
                return null;
            }
        }

        public static void DecryptAesEcb(MemoryStream inputStream, byte[] key, Bhd5Range[] ranges)
        {
            var cipher = CreateAesEcbCipher(key);

            foreach (var range in ranges)
            {
                if (range.StartOffset == -1 || range.EndOffset == -1)
                {
                    continue;
                }

                inputStream.Position = range.StartOffset;
                long length = range.EndOffset - range.StartOffset;
                MemoryStream decryptedStream = DecryptAes(inputStream, cipher, length);
                inputStream.Position = range.StartOffset;
                decryptedStream.WriteTo(inputStream);
            }
        }
    }
}
