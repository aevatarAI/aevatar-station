using System.IO;

namespace ipdb
{
    public class InvalidDatabaseException : IOException
    {
        public InvalidDatabaseException(string message) : base(message)
        {
        }
    }
}