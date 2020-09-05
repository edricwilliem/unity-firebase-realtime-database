using System;

namespace FirebaseREST
{
    public class TokenData
    {
        public string IdToken { get; }
        public string RefreshToken { get; }
        public string ExpiresIn { get; }
        public DateTime RefreshedAt { get; }

        public TokenData(string idToken, string refreshToken, string expiresIn, DateTime refreshedAt) {
            IdToken = idToken;
            RefreshToken = refreshToken;
            ExpiresIn = expiresIn;
            RefreshedAt = refreshedAt;
        }
    }
}