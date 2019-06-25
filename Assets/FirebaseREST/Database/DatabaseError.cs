using System;
namespace FirebaseREST
{
    public class DatabaseError
    {
        FirebaseDatabaseErrorCode code;

        public DatabaseError(FirebaseDatabaseErrorCode code)
        {
            this.code = code;
        }

        public string Message
        {
            get
            {
                return code.ToString();
            }
        }

        public FirebaseDatabaseErrorCode Code
        {
            get
            {
                return code;
            }
        }

        public DatabaseException ToException()
        {
            return new DatabaseException(code.ToString());
        }
    }
}