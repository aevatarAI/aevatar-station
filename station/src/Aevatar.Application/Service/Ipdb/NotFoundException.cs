using System;

namespace ipdb
{
    public class NotFoundException : Exception
    {

        public NotFoundException(string name) : base(name)
        {
        }
    }
}