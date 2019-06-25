using System;
namespace FirebaseREST
{
    public class FirebaseDatabaseErrorEventArgs : EventArgs
    {
        private DatabaseError error;
        public FirebaseDatabaseErrorEventArgs(DatabaseError error)
        {
            this.error = error;
        }
        public DatabaseError DatabaseError { get { return error; } }
    }
}