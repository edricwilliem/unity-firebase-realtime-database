namespace FirebaseREST
{
    public class Response<T> : Response
    {
        public new T data;
        public Response(string message, bool success, int code, T data = default(T)) : base(message, success, code, null)
        {
            this.data = data;
            this.message = message;
            this.success = success;
            this.code = code;
        }
    }

    public class Response
    {
        public string message;
        public bool success;
        public int code;
        public string data;

        public Response(string message, bool success, int code, string data)
        {
            this.message = message;
            this.success = success;
            this.code = code;
            this.data = data;
        }
    }

    public enum ResponseCode
    {
        CANCELLED = -199,
        TIMEOUT = -200,
        SUCCESS = 200,
    }
}