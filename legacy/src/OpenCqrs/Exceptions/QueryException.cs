using System;

namespace OpenCqrs.Exceptions
{
    public class QueryException : Exception
    {
        public QueryException(string errorMessage) : base(errorMessage)
        {
        }
    }
}
