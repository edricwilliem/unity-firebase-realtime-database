using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MiniJSON;
using UnityEngine;
using UnityEngine.Networking;
namespace FirebaseREST
{
    public partial class DatabaseReference : Query
    {
        string reference;
        string orderBy;
        string endAt;
        string startAt;
        string equalTo;
        string limitToFirst;
        string limitToLast;

        EventHandler<ValueChangedEventArgs> _ValueChanged;
        FirebaseServerEventResponse eventResponse;

        UnityWebRequest webReq;
#if UNITY_WEBGL
        FirebaseDatabase.FirebaseEventSourceWebGL esGL;
#endif
        object CacheData;

        int childMovedRefCount = 0, childChangedRefCount = 0, childAddedRefCount = 0, childRemovedRefCount = 0, valueChangedRefCount = 0;

        public override event EventHandler<FirebaseDatabaseErrorEventArgs> DatabaseError;
        public override event EventHandler HeartBeat;
        public event EventHandler Disposed;

        public override event EventHandler<ValueChangedEventArgs> ValueChanged
        {
            add
            {
                valueChangedRefCount++;
                BeginListeningServerEvents();
                _ValueChanged += value;
            }
            remove
            {
                valueChangedRefCount--;
                DisposedUnityWebRequestIfNoReferences();
                _ValueChanged -= value;
            }
        }

        void BeginListeningServerEvents()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (esGL != null) return;
            string url = this.ReferenceUrl;
            if (FirebaseAuth.Instance.IsSignedIn) {
                FirebaseAuth.Instance.GetAccessToken((accessToken) => { 
                    url = url + "?auth=" + accessToken;
                    esGL = new FirebaseDatabase.FirebaseEventSourceWebGL(url, true, null,
                        OnEventSourceMessageReceived, OnEventSourceError);
                });
            } else {
                esGL = new FirebaseDatabase.FirebaseEventSourceWebGL(url, false, null,
                    OnEventSourceMessageReceived, OnEventSourceError);
            }
            
#else
            if (webReq != null) return;
            string url = this.ReferenceUrl;

            Action sendRequest = () => {
                webReq = new UnityWebRequest(url);
                webReq.SetRequestHeader("Accept", "text/event-stream");
                webReq.SetRequestHeader("Cache-Control", "no-cache");
                FirebaseServerEventsDownloadHandler downloadHandler = new FirebaseServerEventsDownloadHandler();
                downloadHandler.DataReceived += OnDataReceived;
                webReq.downloadHandler = downloadHandler;
                webReq.disposeDownloadHandlerOnDispose = true;
                UnityWebRequestAsyncOperation webReqAO = webReq.SendWebRequest();
                webReqAO.completed += ((ao) => OnStopListening(webReqAO));
            };
            
            if (FirebaseAuth.Instance.IsSignedIn) {
                FirebaseAuth.Instance.GetAccessToken(accessToken => {
                    url = url + "?auth=" + accessToken;
                    sendRequest();
                });
            } else
                sendRequest();
#endif
        }

#if UNITY_WEBGL
        private void OnEventSourceError(FirebaseEventSourceErrorArgs obj)
        {
            FirebaseDatabaseErrorCode code;
            try
            {
                code = (FirebaseDatabaseErrorCode)Enum.Parse(typeof(FirebaseDatabaseErrorCode), obj.Error);
                if (DatabaseError != null)
                    DatabaseError(this, new FirebaseDatabaseErrorEventArgs(new DatabaseError(code)));
            }
            catch
            {
                if (DatabaseError != null)
                    DatabaseError(this, new FirebaseDatabaseErrorEventArgs(new DatabaseError(FirebaseDatabaseErrorCode.NetworkError)));
            }
        }

        private void OnEventSourceMessageReceived(FirebaseEventSourceMessageArgs obj)
        {
            OnDataReceived("event: " + obj.EventBuffer + "\ndata: " + obj.DataBuffer);
        }
#endif

        private void OnStopListening(UnityWebRequestAsyncOperation obj)
        {
            if (obj.webRequest.isNetworkError)
            {
                Debug.LogError("Network error");
                if (DatabaseError != null)
                    DatabaseError(this, new FirebaseDatabaseErrorEventArgs(new DatabaseError(FirebaseDatabaseErrorCode.NetworkError)));
            }
            else
            {
                Debug.LogWarning(obj.webRequest.responseCode + "-" + obj.webRequest.downloadHandler.text);
                FirebaseDatabaseErrorCode code;
                try
                {
                    code = (FirebaseDatabaseErrorCode)Enum.Parse(typeof(FirebaseDatabaseErrorCode), obj.webRequest.downloadHandler.text);
                    if (DatabaseError != null)
                        DatabaseError(this, new FirebaseDatabaseErrorEventArgs(new DatabaseError(code)));
                }
                catch
                {
                    if (DatabaseError != null)
                        DatabaseError(this, new FirebaseDatabaseErrorEventArgs(new DatabaseError(FirebaseDatabaseErrorCode.NetworkError)));
                }
            }
            obj.webRequest.Dispose();
        }

        private void OnDataReceived(string data)
        {
            string[] lines = data.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (eventResponse == null || (string.IsNullOrEmpty(eventResponse.eventType) && eventResponse.data != null))
                    eventResponse = new FirebaseServerEventResponse();

                if (string.IsNullOrEmpty(lines[i]) || lines[i].All(char.IsWhiteSpace)) continue;
                string[] arr = Regex.Split(lines[i], ": ");
                if (arr.Length > 1)
                {
                    switch (arr[0])
                    {
                        case "event":
                            eventResponse.eventType = arr[1];
                            break;
                        case "data":
                            switch (eventResponse.eventType)
                            {
                                case "put":
                                    Dictionary<string, object> dataMap = Json.Deserialize(arr[1]) as Dictionary<string, object>;
                                    eventResponse.data = new FirebaseServerEventData(dataMap["path"].ToString(), dataMap["data"]);
                                    ProcessEventPutData(eventResponse.data);
                                    if (HeartBeat != null)
                                        HeartBeat(this, new EventArgs());
                                    break;
                                case "patch":
                                    dataMap = Json.Deserialize(arr[1]) as Dictionary<string, object>;
                                    eventResponse.data = new FirebaseServerEventData(dataMap["path"].ToString(), dataMap["data"]);
                                    ProcessEventPatchData(eventResponse.data);
                                    if (HeartBeat != null)
                                        HeartBeat(this, new EventArgs());
                                    break;
                                case "keep_alive":
                                    if (HeartBeat != null)
                                        HeartBeat(this, new EventArgs());
                                    break;
                                case "auth_revoked":
                                    if (HeartBeat != null)
                                        HeartBeat(this, new EventArgs());
                                    break;
                            }
                            break;
                    }
                }
            }
        }

        void ProcessEventPutData(FirebaseServerEventData eventData)
        {
            if (CacheData == null)
            {
                CacheData = eventData.data;
            }
            else
            {
                string[] paths = eventData.path.Trim('/').Split('/');
                object obj = CacheData;
                string key = paths[paths.Length - 1];
                for (int i = 0; i < paths.Length - 1; i++)
                {
                    Dictionary<string, object> data = Json.Deserialize(Json.Serialize(obj)) as Dictionary<string, object>;
                    obj = data[paths[i]];
                }
                bool isSame = obj == CacheData;
                if (isSame)
                    AssignValue(ref CacheData, key, eventData.data);
                else
                    AssignValue(ref obj, key, eventData.data);
            }
            if (_ValueChanged != null)
            {
                DatabaseReference databaseReference = new DatabaseReference(Reference);
                FirebaseDataSnapshot snapshot = new FirebaseDataSnapshot(databaseReference, CacheData);
                ValueChangedEventArgs args = new ValueChangedEventArgs(snapshot, null);
                _ValueChanged(this, args);
            }
        }

        void ProcessEventPatchData(FirebaseServerEventData eventData)
        {
            if (CacheData == null)
            {
                CacheData = eventData.data;
            }
            else
            {
                string[] paths = eventData.path.Trim('/').Split('/');
                object obj = CacheData;
                object newObj = eventData.data;
                for (int i = 0; i < paths.Length - 1; i++)
                {
                    Dictionary<string, object> data = Json.Deserialize(Json.Serialize(obj)) as Dictionary<string, object>;
                    obj = data[paths[i]];
                }

                Dictionary<string, object> newData = Json.Deserialize(Json.Serialize(newObj)) as Dictionary<string, object>;
                bool isSame = obj == CacheData;
                foreach (string x in newData.Keys)
                {
                    if (isSame)
                        AssignValue(ref CacheData, x, newData[x]);
                    else
                        AssignValue(ref obj, x, newData[x]);
                }
            }
            if (_ValueChanged != null)
            {
                DatabaseReference databaseReference = new DatabaseReference(Reference);
                FirebaseDataSnapshot snapshot = new FirebaseDataSnapshot(databaseReference, CacheData);
                ValueChangedEventArgs args = new ValueChangedEventArgs(snapshot, null);
                _ValueChanged(this, args);
            }
        }

        void AssignValue(ref object data, string dataKey, object value)
        {
            if (!string.IsNullOrEmpty(dataKey))
            {
                Dictionary<string, object> dataMap = data as Dictionary<string, object>;
                if (dataMap != null)
                {
                    dataMap[dataKey] = value;
                }
                else
                {
                    data = new Dictionary<string, object>();
                    ((Dictionary<string, object>)data)[dataKey] = value;
                }
            }
            else
            {
                data = value;
            }
        }

        void DisposedUnityWebRequestIfNoReferences()
        {
            if (valueChangedRefCount == 0 &&
                childAddedRefCount == 0 &&
                childChangedRefCount == 0 &&
                childMovedRefCount == 0 &&
                childRemovedRefCount == 0)
            {
#if UNITY_WEBGL
                if (esGL != null)
                {
                    esGL.Close();
                    esGL = null;
                }
#else
                if (webReq != null)
                {
                    webReq.Dispose();
                    webReq = null;
                }
#endif
            }
        }

        public DatabaseReference(string reference)
        {
            this.reference = reference.Trim('/', ' ');
        }

        public string ReferenceUrl
        {
            get
            {
                return FirebaseSettings.DATABASE_URL + this.reference + ".json";
            }
        }

        public string Reference
        {
            get
            {
                return this.reference;
            }
        }

        public DatabaseReference Child(string node)
        {
            return new DatabaseReference(node.Trim('/', ' '));
        }

        public override Query EndAt(string value)
        {
            this.endAt = "endAt=" + "\"" + value + "\"";
            return this;
        }

        public override Query EndAt(double value)
        {
            this.endAt = "endAt=" + value;
            return this;
        }

        public override Query EndAt(bool value)
        {
            this.endAt = "endAt=" + value;
            return this;
        }

        public override Query EndAt(string value, string key)
        {
            this.orderBy = "orderBy=\"" + key + "\"";
            this.endAt = "endAt=\"" + value + "\"";
            return this;
        }

        public override Query EndAt(double value, string key)
        {
            this.orderBy = "orderBy=\"" + key + "\"";
            this.endAt = "endAt=" + value;
            return this;
        }

        public override Query EndAt(bool value, string key)
        {
            this.orderBy = "orderBy=\"" + key + "\"";
            this.endAt = "endAt=" + value;
            return this;
        }

        public override Query EqualTo(bool value, string key)
        {
            this.orderBy = "orderBy=\"" + key + "\"";
            this.equalTo = "equalTo=" + value;
            return this;
        }

        public override Query EqualTo(double value, string key)
        {
            this.orderBy = "orderBy=\"" + key + "\"";
            this.equalTo = "equalTo=" + value;
            return this;
        }

        public override Query EqualTo(string value, string key)
        {
            this.orderBy = "orderBy=\"" + key + "\"";
            this.equalTo = "equalTo=\"" + value + "\"";
            return this;
        }

        public override Query EqualTo(bool value)
        {
            this.equalTo = "equalTo=" + value;
            return this;
        }

        public override Query EqualTo(double value)
        {
            this.equalTo = "equalTo=" + value;
            return this;
        }

        public override Query EqualTo(string value)
        {
            this.equalTo = "equalTo=\"" + value + "\"";
            return this;
        }

        public override Query LimitToFirst(int limit)
        {
            this.limitToFirst = "limitToFirst=" + limit;
            return this;
        }

        public override Query LimitToLast(int limit)
        {
            this.limitToLast = "limitToLast=" + limit;
            return this;
        }

        public override Query OrderByChild(string path)
        {
            this.orderBy = "orderBy=\"" + path + "\"";
            return this;
        }

        public override Query OrderByKey()
        {
            this.orderBy = "orderBy=\"$key\"";
            return this;
        }

        public override Query OrderByPriority()
        {
            this.orderBy = "orderBy=\"$priority\"";
            return this;
        }

        public override Query OrderByValue()
        {
            this.orderBy = "orderBy=\"$value\"";
            return this;
        }

        public override Query StartAt(bool value, string key)
        {
            this.orderBy = "orderBy=\"" + key + "\"";
            this.startAt = "startAt=" + value;
            return this;
        }

        public override Query StartAt(double value, string key)
        {
            this.orderBy = "orderBy=\"" + key + "\"";
            this.startAt = "startAt=" + value;
            return this;
        }

        public override Query StartAt(string value, string key)
        {
            this.orderBy = "orderBy=\"" + key + "\"";
            this.startAt = "startAt=\"" + value + "\"";
            return this;
        }

        public override Query StartAt(bool value)
        {
            this.startAt = "startAt=" + value;
            return this;
        }

        public override Query StartAt(double value)
        {
            this.startAt = "startAt=" + value;
            return this;
        }

        public override Query StartAt(string value)
        {
            this.startAt = "startAt=\"" + value + "\"";
            return this;
        }

        List<string> GetQueries()
        {
            List<string> queries = new List<string>();
            if (orderBy != null)
                queries.Add(orderBy);
            if (startAt != null)
                queries.Add(startAt);
            if (endAt != null)
                queries.Add(endAt);
            if (equalTo != null)
                queries.Add(equalTo);
            if (limitToFirst != null)
                queries.Add(limitToFirst);
            if (limitToLast != null)
                queries.Add(limitToLast);
            return queries;
        }

        public override void GetValueAsync(int timeout, Action<Response<DataSnapshot>> OnComplete)
        {
            List<string> query = GetQueries();
            string url = this.ReferenceUrl;
            if (query != null)
            {
                url = url + "?" + string.Join("&", query.ToArray());
            }

            UnityWebRequest webReq = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            webReq.downloadHandler = new DownloadHandlerBuffer();
            webReq.timeout = timeout;

            Action sendRequest = () => {
                var op = webReq.SendWebRequest();
                op.completed += (ao) => HandleFirebaseDatabaseResponse(op, res => {
                    OnComplete?.Invoke(new Response<DataSnapshot>(res.message, res.success, res.code, new FirebaseDataSnapshot(this, Json.Deserialize(res.data))));
                });
            };

            if (FirebaseAuth.Instance.IsSignedIn) {
                FirebaseAuth.Instance.GetAccessToken(accessToken => {
                    string sign = query == null ? "?" : "&";
                    webReq.url = webReq.url + sign + "auth=" + accessToken;
                    sendRequest();
                });
            } else {
                sendRequest();
            }
        }

        public void Push(object data, int timeout, Action<Response<string>> OnComplete)
        {
            PushFirebaseData(this.ReferenceUrl, Json.Serialize(data), timeout, OnComplete);
        }

        public void SetRawJsonValueAsync(string json, int timeout, Action<Response> OnComplete)
        {
            try
            {
                object data = Json.Deserialize(json);
                if (data is Dictionary<string, object> || data is List<object>)
                    WriteFirebaseData(this.ReferenceUrl, json, timeout, "PUT", OnComplete);
                else
                    throw new Exception("Not a valid json");

            }
            catch
            {
                throw new Exception("Not a valid json");
            }
        }

        public void SetValueAsync(object data, int timeout, Action<Response> OnComplete)
        {
            try
            {
                data = Json.Serialize(data);
                WriteFirebaseData(this.ReferenceUrl, data, timeout, "PUT", OnComplete);
            }
            catch
            {
                throw new NotSupportedException("Not supported data types");
            }
        }

        public void UpdateChildAsync(Dictionary<string, object> data, int timeout, Action<Response> OnComplete)
        {
            WriteFirebaseData(this.ReferenceUrl, Json.Serialize(data), timeout, "PATCH", OnComplete);
        }

        public void RemoveValueAsync(int timeout, Action<Response> OnComplete)
        {
            UnityWebRequest webReq = new UnityWebRequest(this.ReferenceUrl, "DELETE");
            webReq.downloadHandler = new DownloadHandlerBuffer();
            webReq.SetRequestHeader("Content-Type", "application/json");
            webReq.timeout = timeout;

            Action sendRequest = () => {
                var op = webReq.SendWebRequest();
                op.completed += ((ao) => HandleFirebaseDatabaseResponse(op, OnComplete));
            };

            if (FirebaseAuth.Instance.IsSignedIn) {
                FirebaseAuth.Instance.GetAccessToken((accessToken) => {
                    webReq.url = webReq.url + "?auth=" + accessToken;
                    sendRequest();
                });
            } else {
                sendRequest();
            }
        }

        void PushFirebaseData(string dbpath, string rawData, int timeout, Action<Response<string>> OnComplete)
        {
            UnityWebRequest webReq = new UnityWebRequest(this.ReferenceUrl, "POST");
            webReq.downloadHandler = new DownloadHandlerBuffer();
            byte[] bodyRaw = Encoding.UTF8.GetBytes(rawData);
            webReq.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webReq.SetRequestHeader("Content-Type", "application/json");
            webReq.timeout = timeout;

            Action sendRequest = () => {
                var op = webReq.SendWebRequest();
                op.completed += (ao) => HandleFirebaseDatabaseResponse(op, (res) => {
                    string pushedId = null;
                    if (res.success) {
                        Dictionary<string, object> data = Json.Deserialize(res.data) as Dictionary<string, object>;
                        pushedId = data["name"].ToString();
                    }
                    OnComplete?.Invoke(new Response<string>(res.message, res.success, res.code, pushedId));
                });
            };

            if (FirebaseAuth.Instance.IsSignedIn) {
                FirebaseAuth.Instance.GetAccessToken(accessToken => {
                    webReq.url = webReq.url + "?auth=" + accessToken;
                    sendRequest();
                });
            } else {
                sendRequest();
            }
        }

        void WriteFirebaseData(string dbpath, object data, int timeout, string requestMethod, Action<Response> OnComplete)
        {
            UnityWebRequest webReq = new UnityWebRequest(this.ReferenceUrl, requestMethod);
            webReq.downloadHandler = new DownloadHandlerBuffer();
            byte[] bodyRaw = Encoding.UTF8.GetBytes(data.ToString());
            webReq.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webReq.SetRequestHeader("Content-Type", "application/json");
            webReq.timeout = timeout;

            Action sendRequest = () => {
                var op = webReq.SendWebRequest();
                op.completed += ((ao) => HandleFirebaseDatabaseResponse(op, OnComplete));
            };

            if (FirebaseAuth.Instance.IsSignedIn) {
                FirebaseAuth.Instance.GetAccessToken((accessToken) => {
                    webReq.url = webReq.url + "?auth=" + accessToken;
                    sendRequest();
                });
            } else {
                sendRequest();
            }
        }

        void HandleFirebaseDatabaseResponse(UnityWebRequestAsyncOperation webReqOp, Action<Response> OnComplete)
        {
            if (webReqOp.webRequest.isNetworkError)
            {
                if (OnComplete != null)
                    OnComplete(new Response(webReqOp.webRequest.error, false, 0, null));
            }
            else if (webReqOp.webRequest.isHttpError)
            {
                if (OnComplete != null)
                {
                    Dictionary<string, object> res = Json.Deserialize(webReqOp.webRequest.downloadHandler.text) as Dictionary<string, object>;
                    OnComplete(new Response(res["error"].ToString(), false, (int)webReqOp.webRequest.responseCode, null));
                }
            }
            else
            {
                if (OnComplete != null)
                    OnComplete(new Response("success", true, (int)ResponseCode.SUCCESS, webReqOp.webRequest.downloadHandler.text));
            }
        }

        public override void Dispose()
        {
            if (webReq != null)
                webReq.Dispose();
            CacheData = null;
            _ValueChanged = null;
            if (Disposed != null)
                Disposed(this, new EventArgs());
        }
    }
}