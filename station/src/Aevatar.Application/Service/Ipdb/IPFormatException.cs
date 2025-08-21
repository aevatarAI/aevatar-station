using System;

namespace ipdb
{
    public class IPFormatException : Exception
    {

        public IPFormatException(string name) : base(name)
        {
        }
    }

}