using System;

namespace Tunetoon.Patcher
{
    public class PatchException : Exception
    {
        public PatchException(string message) 
            : base(message)
        {
        }
    }
}
