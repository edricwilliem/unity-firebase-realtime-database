namespace FirebaseREST
{
    public partial class DatabaseReference
    {
        class FirebaseServerEventData
        {

            public string path;
            public object data;
            public FirebaseServerEventData(string path, object data)
            {
                this.path = path;
                this.data = data;
            }
        }
    }
}