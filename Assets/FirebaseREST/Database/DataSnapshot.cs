using System;
using System.Collections.Generic;
using System.Linq;
namespace FirebaseREST
{
    public abstract class DataSnapshot
    {
        protected object data;
        protected DatabaseReference dbReference;

        protected DataSnapshot(DatabaseReference dbReference, object data)
        {
            this.data = data;
            this.dbReference = dbReference;
        }

        public abstract bool HasChildren { get; }

        public abstract bool Exists { get; }

        public abstract object Value { get; }

        public abstract int ChildrenCount { get; }

        public abstract DatabaseReference Reference { get; }

        public abstract string Key { get; }

        public abstract List<DataSnapshot> Children { get; }

        public abstract DataSnapshot Child(string path);

        public abstract string GetRawJsonValue();

        public abstract bool HasChild(string path);
    }
}