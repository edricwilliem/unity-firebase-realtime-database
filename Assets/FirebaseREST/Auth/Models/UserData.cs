using System.Collections.Generic;
namespace FirebaseREST
{
    public class UserData
    {
        ///<summary>The email for the authenticated user.</summary>
        public string email;
        ///<summary>The uid of the authenticated user.</summary>
        public string localId;
        ///<summary>Whether or not the account's email has been verified.</summary>
        public bool emailVerified;
        ///<summary>The display name for the account.</summary>
        public string displayName;
        ///<summary> List of all linked provider</summary>
        public List<ProviderInfo> providerUserInfo;
        ///<summary>The photo Url for the account.</summary>
        public string photoUrl;
        ///<summary>The timestamp, in milliseconds, that the account password was last changed.</summary>
        public long passwordUpdatedAt;
        ///<summary>The timestamp, in seconds, which marks a boundary, before which Firebase ID token are considered revoked.</summary>
        public string validSince;
        ///<summary>Whether the account is disabled or not.</summary>
        public bool disabled;
        ///<summary>The timestamp, in milliseconds, that the account last logged in at.</summary>
        public long lastLoginAt;
        ///<summary>The timestamp, in milliseconds, that the account was created at.</summary>
        public long createdAt;
        ///<summary>Whether the account is authenticated by the developer.</summary>
        public bool customAuth;
    }
}