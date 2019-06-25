namespace FirebaseREST
{
    public partial class DatabaseReference
    {
        sealed class FirebaseServerEventResponse
        {
            public string eventType;
            public FirebaseServerEventData data;
        }
    }
}