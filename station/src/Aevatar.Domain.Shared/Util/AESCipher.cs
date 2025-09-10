namespace Aevatar.Util;

using System;
using System.IO;
using System.Security.Cryptography;

public class AESCipher
{
    private readonly byte[] salt;
    private readonly string password;

    public AESCipher(string password)
    {
        this.salt = GenerateRandomSalt();
        this.password = password;
    }

    private byte[] GenerateRandomSalt()
    {
        byte[] salt = new byte[16]; // 128 bits
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        return salt;
    }

    private byte[] CreateKeyFromPassword()
    {
        using (var kdf = new Rfc2898DeriveBytes(password, salt, 100000)) // 100,000 iterations
        {
            return kdf.GetBytes(32); // AES-256 requires a 32-byte key
        }
    }

    public string Encrypt(string plainText)
    {
        byte[] key = CreateKeyFromPassword();
        
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.GenerateIV();

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream())
            {
                ms.Write(salt, 0, salt.Length); 
                ms.Write(aes.IV, 0, aes.IV.Length);
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public string Decrypt(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        Array.Copy(fullCipher, 0, salt, 0, salt.Length); // 提取盐值
        byte[] key = CreateKeyFromPassword();

        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.Mode = CipherMode.CBC;

            byte[] iv = new byte[aes.IV.Length];
            Array.Copy(fullCipher, salt.Length, iv, 0, iv.Length);

            var cipher = new byte[fullCipher.Length - salt.Length - iv.Length];
            Array.Copy(fullCipher, salt.Length + iv.Length, cipher, 0, cipher.Length);

            using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
            using (var ms = new MemoryStream(cipher))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
}