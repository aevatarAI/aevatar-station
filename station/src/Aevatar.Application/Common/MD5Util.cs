using System;
using System.Security.Cryptography;
using System.Text;

namespace Aevatar.Common;

public class MD5Util
{
    public static string CalculateMD5(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

            return Convert.ToHexString(hash);
        }
    }
}