﻿using kkvpn_client.Communication;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace kkvpn_client
{
    class AesEngineBC : IEncryptionEngine
    {
        private Dictionary<int, AesPeerEngine> Engines;

        public void Initialize()
        {
            Engines = new Dictionary<int, AesPeerEngine>();
        }

        public byte[] Encrypt(byte[] data, int? key)
        {
            if (key == null)
            {
                return null;
            }

            AesPeerEngine engine = Engines[key ?? -1];
            return engine.Encrypt(data);
        }

        public byte[] Decrypt(byte[] data, int? key)
        {
            if (key == null)
            {
                return null;
            }

            AesPeerEngine engine = Engines[key ?? -1];
            return engine.Decrypt(data);
        }

        public int AddKeyToStore(byte[] key)
        {
            if (key == null)
            {
                return -1;
            }
            int dictKey = key.GetHashCode();

            AesPeerEngine engine = new AesPeerEngine(key);
            Engines.Add(dictKey, engine);

            return dictKey;
        }

        public void DeleteKeyIfInStore(int? key)
        {
            if (key == null)
            {
                return;
            }

            if (Engines.ContainsKey(key ?? -1))
            {
                Engines.Remove(key ?? -1);
            }
        }
    }

    class AesPeerEngine
    {
        private const int AesKeySize = 32;
        private const int AesBlockSize = 16;
        private const int SHA1HashSize = 20;

        private SHA1 sha;
        private Random ivGenerator;
        private byte[] _key;

        public AesPeerEngine(byte[] key)
        {
            _key = key;
            ivGenerator = new Random((int)DateTime.Now.Ticks);
            sha = SHA1.Create();
        }

        public byte[] Encrypt(byte[] data)
        {
            byte[] iv = new byte[AesBlockSize];
            ivGenerator.NextBytes(iv);

            PaddedBufferedBlockCipher _encrypt = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesFastEngine()));
            _encrypt.Init(true, new ParametersWithIV(new KeyParameter(_key), iv));
            
            return ConcatanateIvHashAndMessage(iv, ProcessData(_encrypt, data));
        }

        public byte[] Decrypt(byte[] data)
        {
            byte[][] div = DivideIntoIvHashAndMessage(data);

            PaddedBufferedBlockCipher _decrypt = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesFastEngine()));
            _decrypt.Init(false, new ParametersWithIV(new KeyParameter(_key), div[0]));

            if (div[1].SequenceEqual(sha.ComputeHash(div[2])))
                return null;

            return ProcessData(_decrypt, div[2]);
        }

        private byte[] ProcessData(PaddedBufferedBlockCipher cipher, byte[] data)
        {
            int minSize = cipher.GetOutputSize(data.Length);
            byte[] outBuf = new byte[minSize];

            int length1 = cipher.ProcessBytes(data, 0, data.Length, outBuf, 0);
            int length2 = cipher.DoFinal(outBuf, length1);

            int actualLength = length1 + length2;
            byte[] result = new byte[actualLength];

            Array.Copy(outBuf, 0, result, 0, actualLength);
            return result;
        }

        private byte[] ConcatanateIvHashAndMessage(byte[] IV, byte[] message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bs = new BinaryWriter(ms))
                {
                    bs.Write(IV);
                    bs.Write(sha.ComputeHash(message));
                    bs.Write(message);
                }

                return ms.ToArray();
            }
        }

        private byte[][] DivideIntoIvHashAndMessage(byte[] data)
        {
            byte[][] result = new byte[3][];

            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    result[0] = new byte[AesBlockSize];
                    br.Read(result[0], 0, AesBlockSize);

                    result[1] = new byte[SHA1HashSize];
                    br.Read(result[1], 0, SHA1HashSize);

                    result[2] = new byte[data.Length - AesBlockSize - SHA1HashSize];
                    br.Read(result[2], 0, data.Length - AesBlockSize - SHA1HashSize);
                }
            }

            return result;
        }
    }
}
