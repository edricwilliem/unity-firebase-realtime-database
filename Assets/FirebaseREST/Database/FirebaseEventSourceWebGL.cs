using System;
namespace FirebaseREST
{
    public partial class FirebaseDatabase
    {
#if UNITY_WEBGL
        public class FirebaseEventSourceWebGL
        {
            Action<FirebaseEventSourceErrorArgs> _EventSourceError;
            Action<FirebaseEventSourceMessageArgs> _EventSourceMessage;
            Action<FirebaseEventSourceOpenArgs> _EventSourceOpen;
            int _id;
            public int Id
            {
                get { return _id; }
            }

            public Action<FirebaseEventSourceErrorArgs> EventSourceError { get { return _EventSourceError; } }
            public Action<FirebaseEventSourceMessageArgs> EventSourceMessage { get { return _EventSourceMessage; } }
            public Action<FirebaseEventSourceOpenArgs> EventSourceOpen { get { return _EventSourceOpen; } }

            public FirebaseEventSourceWebGL(string url, bool withCredentials, Action<FirebaseEventSourceOpenArgs> onOpen,
              Action<FirebaseEventSourceMessageArgs> onMessage, Action<FirebaseEventSourceErrorArgs> onError)
            {
                this._EventSourceError = onError;
                this._EventSourceMessage = onMessage;
                this._EventSourceOpen = onOpen;
                this._id = CreateEventSource(url, withCredentials, EventSourceOpenCallback, EventSourceMessageCallback, EventSourceErrorCallback);
                esWebGLMap.Add(_id, this);
            }

            public void Close()
            {
                CloseEventSource(_id);
                if (esWebGLMap.ContainsKey(_id))
                {
                    esWebGLMap.Remove(_id);
                }
            }
        }
#endif
    }
}