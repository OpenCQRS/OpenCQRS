using System;

namespace OpenCqrs.Exceptions
{
    public class ValidationException : Exception
    {
        public ValidationException(string errorMessage) : base(errorMessage)
        {
        }
    }
}
