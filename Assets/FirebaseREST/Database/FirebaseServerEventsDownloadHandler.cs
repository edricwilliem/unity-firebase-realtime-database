using System.Text;
using UnityEngine.Networking;
namespace FirebaseREST
{
    public partial class DatabaseReference
    {
        sealed class FirebaseServerEventsDownloadHandler : DownloadHandlerScript
        {
            public delegate void DataReceievedEvent(string data);
            public event DataReceievedEvent DataReceived;
            byte[] bytes;

            protected override byte[] GetData()
            {
                return bytes;
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || data.Length < 1)
                {
                    // Debug.Log("LoggingDownloadHandler :: ReceiveData - received a null/empty buffer");
                    return false;
                }
                this.bytes = data;
                // Debug.Log(string.Format("LoggingDownloadHandler :: ReceiveData - received {0} bytes", dataLength));
                if (DataReceived != null)
                    DataReceived(Encoding.UTF8.GetString(data));
                return true;
            }
        }
    }
}