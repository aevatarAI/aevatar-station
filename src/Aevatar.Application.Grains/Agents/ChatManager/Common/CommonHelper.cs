using System.Security.Cryptography;
using System.Text;

namespace Aevatar.Application.Grains.Agents.ChatManager.Common;

public class CommonHelper
{
    public static Guid StringToGuid(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(hash);
        }
    }

    public static Guid GetSessionManagerConfigurationId()
    {
        return StringToGuid("GetSessionManagerConfigurationId");
    }
}