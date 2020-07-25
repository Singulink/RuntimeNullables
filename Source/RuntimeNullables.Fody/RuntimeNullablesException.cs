using System;

namespace RuntimeNullables.Fody
{
    public class RuntimeNullablesException : Exception
    {
        public RuntimeNullablesException()
        {
        }

        public RuntimeNullablesException(string message) : base(message)
        {
        }

        public RuntimeNullablesException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
