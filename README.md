# Unity C# Firebase Realtime Database (REST API)
Write, Read, Remove and Streaming data using [Firebase's database REST API](https://firebase.google.com/docs/reference/rest/database)

This is not firebase's official plugins library.

Contributions to this project are welcome! 

## Sample Usage

### Setting Firebase

Before using the library you need to setup some settings in `FirebaseSettings.cs`
```
DATABASE_URL = "https://example.firebaseio.com/";
WEB_API = "[WEB_API_KEY]";
```

### Write Data
Set Value:
```
DatabaseReference reference = FirebaseDatabase.Instance.GetReference("path/to/save");
reference.SetValueAsync("mydata", 10,(res) => 
{
    if (res.success)
    {
        Debug.Log("Write success");
    }
    else
    {
        Debug.Log("Write failed : " + res.message);
    }
});

reference.SetRawJsonValueAsync("{\"key\":\"value\"}", 10,(res) => 
{
    if (res.success)
    {
        Debug.Log("Write success");
    }
    else
    {
        Debug.Log("Write failed : " + res.message);
    }
});
```
Update Child Value:
```
DatabaseReference reference = FirebaseDatabase.Instance.GetReference("path/to/save");
reference.UpdateValueAsync(new Dictionary<string, object>(){
    {"child1","value1"},{"child2","value2"}
}, 10, (res) =>
{
    if (res.success)
    {
        Debug.Log("Write success");
    }
    else
    {
        Debug.Log("Write failed : " + res.message);
    }
});
```
Push Value:
```
DatabaseReference reference = FirebaseDatabase.Instance.GetReference("path/to/save");
reference.Push("mydata, 10, (res)=>{
    if(res.success){
        Debug.Log("Pushed with id: " + res.data);
    }
    else{
        Debug.Log("Push failed : " + res.message);
    }
});
```

### Read Data
Get Value:
```
DatabaseReference reference = FirebaseDatabase.Instance.GetReference("path/to/query");
reference.GetValueAsync(10, (res) =>
{
    if (res.success)
    {
        Debug.Log("Success fetched data : " + res.data.GetRawJsonValue());
    }
    else
    {
        Debug.Log("Fetch data failed : " + res.message);
    }
});
```
Query & Order :

* OrderByChild
* OrderByKey
* OrderByValue
* StartAt
* EndAt
* LimitAtFirst
* LimitAtLast
```
DatabaseReference reference = FirebaseDatabase.Instance.GetReference("path/to/query");
reference.OrderByChild("age").StartAt(12).EndAt(20).LimitAtFirst(5).GetValueAsync(10,(res)=>{
        if (res.success)
        {
            Debug.Log("Success fetched data : " +res.data.GetRawJsonValue());
        }
        else
        {
            Debug.Log("Fetch data failed : " + res.message);
        }
});
```

### Delete Data
```
DatabaseReference reference = FirebaseDatabase.Instance.GetReference("path/to/delete");
reference.RemoveValueAsync(10, (e) =>
{
    if (e.success)
    {
        Debug.Log("Delete data success");
    }
    else{
        Debug.Log("Delete data failed : " + res.message);
    }
});
```

### Streaming Data
```
DatabaseReference reference = FirebaseDatabase.Instance.GetReference("path/to/stream");
reference.ValueChanged += (sender, e) =>
{
    Debug.Log(e.Snapshot.GetRawJsonValue());
};
reference.DatabaseError += (sender,e)=>{
    Debug.Log(e.DatabaseError.Message);
    Debug.Log("Streaming connection closed");
};
```

### Authentication
Set the credential using saved tokens

```
FirebaseAuth.Instance.TokenData = new TokenData()
{
    refreshToken = savedRefreshToken,
    idToken = savedAccessToken
};
```

or Sign In
```
FirebaseAuth.Instance.SignInWithEmail("example@example.com", "example", 10, (res) =>
{
    if (res.success)
    {
        Debug.Log("Access token : " + res.data.idToken);
    }
    else
    {
        Debug.Log("Signed Failed");
    }
});
```
after signed in, the `FirebaseAuth.Instance.TokenData` will be automatically set 


`FirebaseAuth.Instance.TokenData`  will be used for authentication on every database's request.

## License
MIT