using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using AOT;
using UnityEngine;
using UnityEngine.Networking;
namespace FirebaseREST
{
    public enum FirebaseDatabaseErrorCode
    {
        OperationFailed = -2,
        UnknownError = -999,
        WriteCanceled = -25,
        NetworkError = -24,
        Unavailable = -10,
        OverriddenBySet = -9,
        UserCodeException = -11,
        InvalidToken = -7,
        ExpiredToken = -6,
        Disconnected = -4,
        PermissionDenied = -3,
        MaxRetries = -8,
    }

    public partial class FirebaseDatabase : MonoBehaviour
    {
        public const string SERVER_VALUE_TIMESTAMP = "{\".sv\": \"timestamp\"}";
        Dictionary<string, DatabaseReference> databaseReferenceMap = new Dictionary<string, DatabaseReference>();

        private static bool applicationIsQuitting = false;
        private static FirebaseDatabase _instance;
        private static object _lock = new object();
        public static FirebaseDatabase Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (FirebaseDatabase)FindObjectOfType(typeof(FirebaseDatabase));

                        if (FindObjectsOfType(typeof(FirebaseDatabase)).Length > 1)
                        {
                            Debug.LogError("there should never be more than 1 singleton!");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<FirebaseDatabase>();
                            singleton.name = "(singleton) " + typeof(FirebaseDatabase).ToString();
                            DontDestroyOnLoad(singleton);
                            Debug.Log(typeof(FirebaseDatabase).ToString() + " singleton created");
                        }
                        else
                        {
                            Debug.Log("instance already created: " + _instance.gameObject.name);
                        }
                    }
                    return _instance;
                }
            }
        }

        public DatabaseReference GetReference(string url)
        {
            url = url.Trim('/', ' ');
            if (databaseReferenceMap.ContainsKey(url))
                return databaseReferenceMap[url];
            DatabaseReference reference = new DatabaseReference(url);
            return reference;
        }

        void OnDestroy()
        {
            applicationIsQuitting = true;
            foreach (DatabaseReference dbRef in databaseReferenceMap.Values)
            {
                if (dbRef != null)
                    dbRef.Dispose();
            }
        }

        delegate void _EventSourceOpenArgs(int id);
        delegate void _EventSourceMessageArgs(int id, string eventBuffer, string dataBuffer);
        delegate void _EventSourceErrorArgs(int id, string errorMessage);
        delegate void _EventSourceClosedArgs(int id);

#if UNITY_WEBGL
        static Dictionary<int, FirebaseEventSourceWebGL> esWebGLMap = new Dictionary<int, FirebaseEventSourceWebGL>();

        [MonoPInvokeCallback(typeof(_EventSourceOpenArgs))]
        public static void EventSourceOpenCallback(int id)
        {
            Debug.Log("Unity ES Open");
            if (esWebGLMap.ContainsKey(id))
            {
                if (esWebGLMap[id].EventSourceOpen != null)
                    esWebGLMap[id].EventSourceOpen(new FirebaseEventSourceOpenArgs());
            }
        }

        [MonoPInvokeCallback(typeof(_EventSourceMessageArgs))]
        public static void EventSourceMessageCallback(int id, string eventBuffer, string dataBuffer)
        {
            Debug.Log("Unity ES Message");
            if (esWebGLMap.ContainsKey(id))
            {
                if (esWebGLMap[id].EventSourceMessage != null)
                    esWebGLMap[id].EventSourceMessage(new FirebaseEventSourceMessageArgs(eventBuffer, dataBuffer));
            }
        }

        [MonoPInvokeCallback(typeof(_EventSourceErrorArgs))]
        public static void EventSourceErrorCallback(int id, string error)
        {
            Debug.Log("Unity ES Error");
            if (esWebGLMap.ContainsKey(id))
            {
                if (esWebGLMap[id].EventSourceError != null)
                    esWebGLMap[id].EventSourceError(new FirebaseEventSourceErrorArgs(error));
                esWebGLMap[id].Close();
                esWebGLMap[id] = null;
                esWebGLMap.Remove(id);
            }
        }

        [DllImport("__Internal")]
        static extern int CreateEventSource(string urlPtr, bool withCredentials, _EventSourceOpenArgs onOpen,
             _EventSourceMessageArgs onMessage, _EventSourceErrorArgs onError);

        [DllImport("__Internal")]
        static extern void CloseEventSource(int id);
#endif
    }
}