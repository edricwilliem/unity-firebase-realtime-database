
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MiniJSON;
using UnityEngine;
using UnityEngine.Networking;
namespace FirebaseREST
{
    public class FirebaseAuth : MonoBehaviour
    {
        readonly string EMAIL_AUTH_URL = "https://www.googleapis.com/identitytoolkit/v3/relyingparty/verifyPassword?key=" + FirebaseSettings.WEB_API;
        readonly string CUSTOM_TOKEN_AUTH_URL = "https://www.googleapis.com/identitytoolkit/v3/relyingparty/verifyCustomToken?key=" + FirebaseSettings.WEB_API;
        readonly string ANONYMOUS_AUTH_URL = "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + FirebaseSettings.WEB_API;
        readonly string REFRESH_TOKEN_URL = "https://securetoken.googleapis.com/v1/token?key=" + FirebaseSettings.WEB_API;
        readonly string USER_INFO_URL = "https://www.googleapis.com/identitytoolkit/v3/relyingparty/getAccountInfo?key=" + FirebaseSettings.WEB_API;
        TokenData tokenData;

        public TokenData TokenData { get { return tokenData; } set { this.tokenData = value; } }

        private static bool applicationIsQuitting = false;
        private static FirebaseAuth _instance;
        private static object _lock = new object();
        public static FirebaseAuth Instance
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
                        _instance = (FirebaseAuth)FindObjectOfType(typeof(FirebaseAuth));

                        if (FindObjectsOfType(typeof(FirebaseAuth)).Length > 1)
                        {
                            Debug.LogError("there should never be more than 1 singleton!");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<FirebaseAuth>();
                            singleton.name = "(singleton) " + typeof(FirebaseAuth).ToString();
                            DontDestroyOnLoad(singleton);
                            Debug.Log(typeof(FirebaseAuth).ToString() + " singleton created");
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

        void OnDestroy()
        {
            applicationIsQuitting = true;
        }
        
        public bool IsSignedIn => tokenData != null;

        public void GetAccessToken(Action<string> onComplete) {
            if (tokenData == null)
                onComplete?.Invoke(null);
            else if (IsTokenExpired)
                RefreshAccessToken(10, response => onComplete(response.data.IdToken));
            else
                onComplete?.Invoke(tokenData.IdToken);
        }

        private bool IsTokenExpired => DateTime.Now - tokenData.RefreshedAt > TimeSpan.FromSeconds(double.Parse(tokenData.ExpiresIn));


        public void FetchUserInfo(int timeout, Action<Response<List<UserData>>> OnComplete)
        {
            if (tokenData == null)
                throw new Exception("User has not logged in");
            UnityWebRequestAsyncOperation op = StartRequest(USER_INFO_URL, "POST", new Dictionary<string, object>(){
            {"idToken",tokenData.IdToken}
        }, timeout);
            op.completed += ((ao) => HandleFirebaseResponse(op, (res) =>
            {
                if (res.success)
                {
                    Dictionary<string, object> map = Json.Deserialize(res.data) as Dictionary<string, object>;
                    List<object> userDatas = Json.Deserialize(Json.Serialize(map["users"])) as List<object>;
                    List<UserData> dataToReturn = new List<UserData>();
                    for (int i = 0; i < userDatas.Count; i++)
                    {
                        Dictionary<string, object> userMap = Json.Deserialize(Json.Serialize(userDatas[i])) as Dictionary<string, object>;
                        UserData ud = new UserData();
                        ud.createdAt = userMap.ContainsKey("createdAt") ? long.Parse(userMap["createdAt"].ToString()) : 0L;
                        ud.customAuth = userMap.ContainsKey("customAuth") ? bool.Parse(userMap["customAuth"].ToString()) : false;
                        ud.disabled = userMap.ContainsKey("disabled") ? bool.Parse(userMap["disabled"].ToString()) : false;
                        ud.displayName = userMap.ContainsKey("displayName") ? userMap["displayName"].ToString() : null;
                        ud.email = userMap.ContainsKey("email") ? userMap["email"].ToString() : null;
                        ud.emailVerified = userMap.ContainsKey("emailVerified") ? bool.Parse(userMap["emailVerified"].ToString()) : false;
                        ud.lastLoginAt = userMap.ContainsKey("lastLoginAt") ? long.Parse(userMap["lastLoginAt"].ToString()) : 0L;
                        ud.localId = userMap.ContainsKey("localId") ? userMap["localId"].ToString() : null;
                        ud.passwordUpdatedAt = userMap.ContainsKey("passwordUpdatedAt") ? long.Parse(userMap["passwordUpdatedAt"].ToString()) : 0L;
                        ud.photoUrl = userMap.ContainsKey("photoUrl") ? userMap["photoUrl"].ToString() : null;
                        ud.validSince = userMap.ContainsKey("photoUrl") ? userMap["validSince"].ToString() : null;
                        if (userMap.ContainsKey("providerUserInfo"))
                        {
                            ud.providerUserInfo = new List<ProviderInfo>();
                            List<object> providers = Json.Deserialize(Json.Serialize(userMap["providerUserInfo"])) as List<object>;
                            for (int j = 0; j < providers.Count; j++)
                            {
                                ProviderInfo providerInfo = new ProviderInfo();
                                Dictionary<string, object> obj = Json.Deserialize(Json.Serialize(providers[j])) as Dictionary<string, object>;
                                providerInfo.federatedId = obj["federatedId"].ToString();
                                providerInfo.providerId = obj["providerId"].ToString();
                                ud.providerUserInfo.Add(providerInfo);
                            }
                        }
                        dataToReturn.Add(ud);
                    }
                    if (OnComplete != null)
                        OnComplete(new Response<List<UserData>>("success", true, (int)op.webRequest.responseCode, dataToReturn));
                }
                else
                {
                    if (OnComplete != null)
                        OnComplete(new Response<List<UserData>>(res.message, false, res.code, null));
                }
            }));
        }

        public void RefreshAccessToken(int timeout, Action<Response<TokenData>> OnComplete)
        {
            if (tokenData == null)
                throw new Exception("User has not logged in");
            UnityWebRequestAsyncOperation op = StartRequest(REFRESH_TOKEN_URL, "POST", new Dictionary<string, object>(){
            {"grant_type","refresh_token"},{"refresh_token",tokenData.RefreshToken}
        }, timeout);
            op.completed += ((ao) => HandleFirebaseResponse(op, (res) =>
            {
                if (res.success)
                {
                    Dictionary<string, object> dataMap = Json.Deserialize(op.webRequest.downloadHandler.text) as Dictionary<string, object>;
                    tokenData = new TokenData(dataMap["id_token"].ToString(), dataMap["refresh_token"].ToString(), 
                        dataMap["expires_in"].ToString(), DateTime.Now);
                    if (OnComplete != null)
                        OnComplete(new Response<TokenData>("success", true, (int)op.webRequest.responseCode, tokenData));
                }
                else
                {
                    if (OnComplete != null)
                        OnComplete(new Response<TokenData>(res.message, false, res.code, null));
                }
            }));
        }

        public void SignInWithCustomToken(string customToken, int timeout, Action<Response<TokenData>> OnComplete)
        {
            UnityWebRequestAsyncOperation op = StartRequest(CUSTOM_TOKEN_AUTH_URL, "POST", new Dictionary<string, object>(){
            {"token",customToken},{"returnSecureToken",true}
        }, timeout);
            op.completed += ((ao) => HandleFirebaseSignInResponse(op, OnComplete));
        }

        public void SignInWithEmail(string email, string password, int timeout, Action<Response<TokenData>> OnComplete)
        {
            UnityWebRequestAsyncOperation op = StartRequest(EMAIL_AUTH_URL, "POST", new Dictionary<string, object>(){
            {"email",email},{"password",password},{"returnSecureToken",true}
        }, timeout);
            op.completed += ((ao) => HandleFirebaseSignInResponse(op, OnComplete));
        }
        
        public void SignInAnonymously(int timeout, Action<Response<TokenData>> OnComplete)
        {
            UnityWebRequestAsyncOperation op = StartRequest(ANONYMOUS_AUTH_URL, "POST", new Dictionary<string, object>(){
                    {"returnSecureToken",true}
            }, timeout);
            op.completed += ((ao) => HandleFirebaseSignInResponse(op, OnComplete));
        }

        UnityWebRequestAsyncOperation StartRequest(string url, string requestMethod, Dictionary<string, object> data, int timeout)
        {
            UnityWebRequest webReq = new UnityWebRequest(url, requestMethod);
            webReq.downloadHandler = new DownloadHandlerBuffer();
            byte[] bodyRaw = Encoding.UTF8.GetBytes(Json.Serialize(data));
            webReq.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webReq.SetRequestHeader("Content-Type", "application/json");
            webReq.timeout = timeout;
            return webReq.SendWebRequest();
        }

        void HandleFirebaseSignInResponse(UnityWebRequestAsyncOperation webReqOp, Action<Response<TokenData>> OnComplete)
        {
            if (webReqOp.webRequest.isNetworkError)
            {
                if (OnComplete != null)
                    OnComplete(new Response<TokenData>(webReqOp.webRequest.error, false, 0, null));
            }
            else if (webReqOp.webRequest.isHttpError)
            {
                Dictionary<string, object> res = Json.Deserialize(webReqOp.webRequest.downloadHandler.text) as Dictionary<string, object>;
                Dictionary<string, object> errorObj = Json.Deserialize(Json.Serialize(res["error"])) as Dictionary<string, object>;
                if (OnComplete != null)
                    OnComplete(new Response<TokenData>(errorObj["message"].ToString(), false, int.Parse(errorObj["code"].ToString()), null));
            }
            else
            {
                if (OnComplete != null)
                {
                    Dictionary<string, object> dataMap = Json.Deserialize(webReqOp.webRequest.downloadHandler.text) as Dictionary<string, object>;
                    this.tokenData = new TokenData(dataMap["idToken"].ToString(), dataMap["refreshToken"].ToString(), dataMap["expiresIn"].ToString(), DateTime.Now);
                    OnComplete(new Response<TokenData>("success", true, (int)webReqOp.webRequest.responseCode, tokenData));
                }
            }
        }

        void HandleFirebaseResponse(UnityWebRequestAsyncOperation webReqOp, Action<Response> OnComplete)
        {
            if (webReqOp.webRequest.isNetworkError)
            {
                if (OnComplete != null)
                    OnComplete(new Response(webReqOp.webRequest.error, false, 0, null));
            }
            else if (webReqOp.webRequest.isHttpError)
            {
                Dictionary<string, object> res = Json.Deserialize(webReqOp.webRequest.downloadHandler.text) as Dictionary<string, object>;
                Dictionary<string, object> errorObj = Json.Deserialize(Json.Serialize(res["error"])) as Dictionary<string, object>;
                if (OnComplete != null)
                    OnComplete(new Response(errorObj["message"].ToString(), false, int.Parse(errorObj["code"].ToString()), null));
            }
            else
            {
                if (OnComplete != null)
                    OnComplete(new Response("success", true, (int)webReqOp.webRequest.responseCode, webReqOp.webRequest.downloadHandler.text));
            }
        }
    }
}