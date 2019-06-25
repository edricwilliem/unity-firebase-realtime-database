
using System;
namespace FirebaseREST
{
    public sealed class DatabaseException : Exception
    {
        public DatabaseException(string message) : base(message)
        {

        }
    }
}