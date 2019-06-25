using System;
namespace FirebaseREST
{
    public class ValueChangedEventArgs : EventArgs
    {
        private DataSnapshot snapshot;
        private DatabaseError error;
        public ValueChangedEventArgs(DataSnapshot snapshot, DatabaseError error)
        {
            this.snapshot = snapshot;
            this.error = error;
        }
        public DataSnapshot Snapshot { get { return snapshot; } }
        public DatabaseError DatabaseError { get { return error; } }
    }
}