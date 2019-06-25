using System;
namespace FirebaseREST
{
    public abstract class Query : IDisposable
    {
        public abstract event EventHandler<ValueChangedEventArgs> ValueChanged;
        public abstract event EventHandler<FirebaseDatabaseErrorEventArgs> DatabaseError;
        public abstract event EventHandler HeartBeat;
        public abstract void Dispose();
        public abstract Query EndAt(string value);
        public abstract Query EndAt(double value);
        public abstract Query EndAt(bool value);
        public abstract Query EndAt(string value, string key);
        public abstract Query EndAt(double value, string key);
        public abstract Query EndAt(bool value, string key);
        public abstract Query EqualTo(bool value, string key);
        public abstract Query EqualTo(double value, string key);
        public abstract Query EqualTo(string value, string key);
        public abstract Query EqualTo(bool value);
        public abstract Query EqualTo(double value);
        public abstract Query EqualTo(string value);
        public abstract void GetValueAsync(int timeout, Action<Response<DataSnapshot>> OnComplete);
        public abstract Query LimitToFirst(int limit);
        public abstract Query LimitToLast(int limit);
        public abstract Query OrderByChild(string path);
        public abstract Query OrderByKey();
        public abstract Query OrderByPriority();
        public abstract Query OrderByValue();
        public abstract Query StartAt(bool value, string key);
        public abstract Query StartAt(double value, string key);
        public abstract Query StartAt(string value, string key);
        public abstract Query StartAt(bool value);
        public abstract Query StartAt(double value);
        public abstract Query StartAt(string value);
    }
}