using System.Collections.Generic;
using System.Linq;
using MiniJSON;
namespace FirebaseREST
{
    public partial class DatabaseReference
    {
        sealed class FirebaseDataSnapshot : DataSnapshot
        {
            private List<DataSnapshot> children;
            private bool alreadyParsed;

            public FirebaseDataSnapshot(DatabaseReference dbReference, object data) : base(dbReference, data)
            {
                this.dbReference = dbReference;
                this.data = data;
            }

            public override bool HasChildren
            {
                get
                {
                    if (data == null) return false;
                    Dictionary<string, object> map = Json.Deserialize(Json.Serialize(data)) as Dictionary<string, object>;
                    if (map == null) return false;
                    return map.Values.First() != null;
                }
            }

            public override bool Exists
            {
                get
                {
                    return data != null;
                }
            }

            public override object Value
            {
                get
                {
                    return data;
                }
            }

            public override int ChildrenCount
            {
                get
                {
                    if (data == null)
                        return 0;
                    Dictionary<string, object> map = Json.Deserialize(data.ToString()) as Dictionary<string, object>;
                    if (map == null) return 0;
                    return map.Values.ToList().Count;
                }
            }

            public override DatabaseReference Reference
            {
                get
                {
                    return dbReference;
                }
            }

            public override string Key
            {
                get
                {
                    string[] arr = dbReference.Reference.Split('/');
                    return arr[arr.Length - 1];
                }
            }

            public override List<DataSnapshot> Children
            {
                get
                {
                    if (children != null) return this.children;
                    if (data == null) return null;

                    List<DataSnapshot> snapshot = new List<DataSnapshot>();
                    Dictionary<string, object> map = Json.Deserialize(Json.Serialize(data)) as Dictionary<string, object>;
                    if (map == null) return snapshot;
                    foreach (string key in map.Keys)
                    {
                        string referencePath = dbReference.Reference.TrimEnd('/', ' ') + "/" + key;
                        snapshot.Add(new FirebaseDataSnapshot(new DatabaseReference(referencePath), map[key].ToString()));
                    }
                    this.children = snapshot;
                    return snapshot;
                }
            }

            public override DataSnapshot Child(string path)
            {
                if (HasChild(path))
                {
                    string referencePath = dbReference.Reference.TrimEnd('/', ' ') + "/" + path;
                    Dictionary<string, object> map = Json.Deserialize(Json.Serialize(data)) as Dictionary<string, object>;
                    return new FirebaseDataSnapshot(new DatabaseReference(referencePath), map[path].ToString());
                }
                else
                {
                    return null;
                }
            }

            public override string GetRawJsonValue()
            {
                if (data == null) return null;
                return Json.Serialize(data);
            }

            public override bool HasChild(string path)
            {
                if (data == null) return false;
                Dictionary<string, object> map = Json.Deserialize(Json.Serialize(data)) as Dictionary<string, object>;
                if (map == null) return false;
                return map.ContainsKey(path);
            }
        }
    }
}