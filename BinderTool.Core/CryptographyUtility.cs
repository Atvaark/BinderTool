using System;
using System.IO;
using BinderTool.Core.Bdt5;
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
        public static Stream DecryptAesEcb(Stream inputStream, byte[] key)
        {
            var cipher = CreateAesEcbCipher(key);
            return DecryptAes(inputStream, cipher, inputStream.Length);
        }

        public static Stream DecryptAesCbc(Stream inputStream, byte[] key, byte[] iv)
        {
            var engine = new CbcBlockCipher(new AesEngine());
            KeyParameter keyParameter = new KeyParameter(key);
            ICipherParameters parameters = new ParametersWithIV(keyParameter, iv);

            engine.Init(false, parameters);
            return DecryptAes(inputStream, engine, inputStream.Length);
        }

        public static Stream DecryptAesCtr(Stream inputStream, byte[] key, byte[] iv)
        {
            SicBlockCipher engine = new SicBlockCipher(new AesEngine());
            KeyParameter keyParameter = new KeyParameter(key);
            ICipherParameters parameters = new ParametersWithIV(keyParameter, iv);

            engine.Init(false, parameters);
            return DecryptAes(inputStream, engine, inputStream.Length);
        }

        private static IBlockCipher CreateAesEcbCipher(byte[] key)
        {
            AesEngine engine = new AesEngine();
            KeyParameter parameter = new KeyParameter(key);
            engine.Init(false, parameter);
            return engine;
        }

        private static Stream DecryptAes(Stream inputStream, IBlockCipher cipher, long length)
        {
            int blockSize = cipher.GetBlockSize();
            long inputLength = length;
            long paddedLength = inputLength;
            if (paddedLength % blockSize > 0)
            {
                paddedLength += blockSize - paddedLength % blockSize;
            }

            byte[] input = new byte[blockSize];
            byte[] output = new byte[blockSize];
            var outputStream = new Bdt5.Bdt5InnerStream(paddedLength);

            for (long pos = 0; pos < inputLength; pos += blockSize)
            {
                int read = inputStream.Read(input, 0, blockSize);
                if (read < blockSize)
                {
                    for (int i = read; i < blockSize; i++) input[i] = 0;
                }
                cipher.ProcessBlock(input, 0, output, 0);
                outputStream.Write(output, 0, blockSize);
            }

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

        public static void DecryptAesEcb(Stream inputStream, byte[] key, Bhd5Range[] ranges)
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
                Stream decryptedStream = DecryptAes(inputStream, cipher, length);
                inputStream.Position = range.StartOffset;
                byte[] buf = new byte[1000];
                for (long pos = 0; pos < length; pos += buf.Length)
                {
                    int read = decryptedStream.Read(buf, 0, 1000);
                    inputStream.Write(buf, 0, read);
                }
            }
        }
    }
}
