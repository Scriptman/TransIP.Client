using System;

namespace TransIP.Client.Exceptions
{
    public class TransIpNotAcceptableException : Exception
    {
        public TransIpNotAcceptableException(string? message) : base(message)
        {
        }
    }
}
