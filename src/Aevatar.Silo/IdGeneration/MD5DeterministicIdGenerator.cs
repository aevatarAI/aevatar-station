using System.Text;
using System.Security.Cryptography;

namespace Aevatar.Silo.IdGeneration
{
    /// <summary>
    /// Default implementation of IDeterministicIdGenerator using MD5
    /// </summary>
    public class MD5DeterministicIdGenerator : IDeterministicIdGenerator
    {
        public Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("Input cannot be null or empty", nameof(input));
            }
            
            // Use a simple and consistent way to convert the type name to a GUID
            using (var md5 = MD5.Create())
            {
                // Get MD5 hash of the input string
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                
                // MD5 produces a 16-byte hash which is exactly the size of a GUID
                return new Guid(hashBytes);
            }
        }
    }
} 