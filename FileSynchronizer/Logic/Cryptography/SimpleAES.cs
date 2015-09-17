using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;

namespace FileSynchronizer.Logic.Cryptography
{
    public class SimpleAES
    {
        private readonly byte[] key =
            {
                225, 193, 204, 179, 94, 2, 235, 3, 217, 36, 254, 173, 243, 106, 167, 131, 186, 43, 91, 196, 57, 104, 171, 17,
                224, 98, 201, 27, 72, 89, 95, 165
            };

        private readonly byte[] vector = { 191, 196, 89, 137, 17, 232, 199, 56, 251, 7, 89, 153, 160, 166, 36, 2 };

        private readonly ICryptoTransform encryptorTransform;

        private readonly ICryptoTransform decryptorTransform;

        private readonly System.Text.UTF8Encoding utfEncoder;

        public SimpleAES()
        {
            var rm = new RijndaelManaged();

            this.encryptorTransform = rm.CreateEncryptor(this.key, this.vector);
            this.decryptorTransform = rm.CreateDecryptor(this.key, this.vector);

            this.utfEncoder = new System.Text.UTF8Encoding();
        }

        public string EncryptToString(string textValue)
        {
            return this.ByteArrToString(this.Encrypt(textValue));
        }

        private byte[] Encrypt(string textValue)
        {
            Byte[] bytes = this.utfEncoder.GetBytes(textValue);

            var memoryStream = new MemoryStream();

            var cs = new CryptoStream(memoryStream, this.encryptorTransform, CryptoStreamMode.Write);
            cs.Write(bytes, 0, bytes.Length);
            cs.FlushFinalBlock();

            memoryStream.Position = 0;
            var encrypted = new byte[memoryStream.Length];
            memoryStream.Read(encrypted, 0, encrypted.Length);

            cs.Close();
            memoryStream.Close();

            return encrypted;
        }

        public string DecryptString(string encryptedString)
        {
            return this.Decrypt(this.StrToByteArray(encryptedString));
        }

        private string Decrypt(byte[] encryptedValue)
        {
            var encryptedStream = new MemoryStream();
            var decryptStream = new CryptoStream(encryptedStream, this.decryptorTransform, CryptoStreamMode.Write);
            decryptStream.Write(encryptedValue, 0, encryptedValue.Length);
            decryptStream.FlushFinalBlock();

            encryptedStream.Position = 0;
            var decryptedBytes = new Byte[encryptedStream.Length];
            encryptedStream.Read(decryptedBytes, 0, decryptedBytes.Length);
            encryptedStream.Close();
            return this.utfEncoder.GetString(decryptedBytes);
        }

        private byte[] StrToByteArray(string str)
        {
            if (str.Length == 0)
            {
                throw new Exception("Invalid string value in StrToByteArray");
            }

            var byteArr = new byte[str.Length / 3];
            int i = 0;
            int j = 0;

            do
            {
                var val = byte.Parse(str.Substring(i, 3));
                byteArr[j++] = val;
                i += 3;
            }
            while (i < str.Length);
            return byteArr;
        }

        private string ByteArrToString(byte[] byteArr)
        {
            string tempStr = "";
            for (var i = 0; i <= byteArr.GetUpperBound(0); i++)
            {
                var val = byteArr[i];

                if (val < 10)
                {
                    tempStr += "00" + val;
                }
                else if (val < 100)
                {
                    tempStr += "0" + val;
                }
                else
                {
                    tempStr += val.ToString(CultureInfo.InvariantCulture);
                }
            }
            return tempStr;
        }
    }
}