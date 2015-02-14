using System;
using System.IO;
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
            AesEngine engine = new AesEngine();
            KeyParameter parameter = new KeyParameter(key);

            BufferedBlockCipher cipher = new BufferedBlockCipher(new CbcBlockCipher(engine));
            cipher.Init(false, parameter);

            return DecryptAes(inputStream, cipher);
        }

        public static MemoryStream DecryptAesCbc(Stream inputStream, byte[] key, byte[] iv)
        {
            AesEngine engine = new AesEngine();
            KeyParameter keyParameter = new KeyParameter(key);
            ICipherParameters parameters = new ParametersWithIV(keyParameter, iv);

            BufferedBlockCipher cipher = new BufferedBlockCipher(new CbcBlockCipher(engine));
            cipher.Init(true, parameters);
            return DecryptAes(inputStream, cipher);
        }

        public static MemoryStream DecryptAesCtrDsII(Stream inputStream, byte[] key, byte[] iv)
        {
            byte[] ivNew = new byte[16];
            ivNew[0] = 128;
            ivNew[12] = 0;
            ivNew[13] = 0;
            ivNew[14] = 0;
            ivNew[15] = 0;
            // TODO: Analyze how the game calculates the iv. Maybe it is salted?

            return DecryptAesCtr(inputStream, key, ivNew);
        }

        public static MemoryStream DecryptAesCtr(Stream inputStream, byte[] key, byte[] iv)
        {
            AesEngine engine = new AesEngine();
            KeyParameter keyParameter = new KeyParameter(key);
            ICipherParameters parameters = new ParametersWithIV(keyParameter, iv);

            BufferedBlockCipher cipher = new BufferedBlockCipher(new SicBlockCipher(engine));
            cipher.Init(false, parameters);
            return DecryptAes(inputStream, cipher);
        }

        private static MemoryStream DecryptAes(Stream inputStream, BufferedBlockCipher cipher)
        {
            byte[] input = new byte[inputStream.Length];
            byte[] output = new byte[cipher.GetOutputSize((int) inputStream.Length)];

            inputStream.Read(input, 0, (int) inputStream.Length);

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
        /// <param name="keyPath">A file containing a key in PEM format</param>
        /// <exception cref="ArgumentNullException">When the argument filePath is null</exception>
        /// <exception cref="ArgumentNullException">When the argument keyPath is null</exception>
        /// <returns>A memory stream with the decrypted file</returns>
        public static MemoryStream DecryptRsa(string filePath, string keyPath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }
            if (keyPath == null)
            {
                throw new ArgumentNullException("keyPath");
            }

            TextReader pemTextReader = File.OpenText(keyPath);
            PemReader pemReader = new PemReader(pemTextReader);
            AsymmetricKeyParameter keyParameter = (AsymmetricKeyParameter) pemReader.ReadObject();

            RsaEngine engine = new RsaEngine();

            engine.Init(false, keyParameter);

            FileStream inputStream = File.OpenRead(filePath);
            MemoryStream outputStream = new MemoryStream();
            int inputBlockSize = engine.GetInputBlockSize();
            int outputBlockSize = engine.GetOutputBlockSize();
            byte[] inputBlock = new byte[inputBlockSize];
            byte[] outputBlock = new byte[outputBlockSize];
            int readBlockSize;
            while ((readBlockSize = inputStream.Read(inputBlock, 0, inputBlock.Length)) > 0)
            {
                outputBlock = engine.ProcessBlock(inputBlock, 0, inputBlockSize);

                int requiredPadding = outputBlockSize - outputBlock.Length;
                if (requiredPadding > 0)
                {
                    byte[] paddedOutputBlock = new byte[outputBlockSize];
                    outputBlock.CopyTo(paddedOutputBlock, requiredPadding);
                    outputBlock = paddedOutputBlock;
                }

                outputStream.Write(outputBlock, 0, outputBlock.Length);
            }
            outputStream.Seek(0, SeekOrigin.Begin);
            return outputStream;
        }
    }
}
