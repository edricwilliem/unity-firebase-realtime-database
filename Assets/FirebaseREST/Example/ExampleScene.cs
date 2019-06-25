using System.Collections;
using System.Collections.Generic;
using FirebaseREST;
using MiniJSON;
using UnityEngine;
using UnityEngine.UI;

public class ExampleScene : MonoBehaviour
{
    public InputField dataIF, pathIF, queryPathIF, orderByIF, startAtIF, endAtIF, equalToIF, limitToFirstIF, limitToLastIF;
    public Dropdown orderByDropDown;
    public Button saveButton, pushButton, listenButton;
    public GameObject contentItem, container1, container2;
    public Text resultText;

    // Use this for initialization
    void Start()
    {
        orderByDropDown.onValueChanged.AddListener((index) =>
        {
            if (index == 3)
            {
                orderByIF.gameObject.SetActive(true);
            }
            else
            {
                orderByIF.gameObject.SetActive(false);
            }
        });
    }

    public void OnButtonClick(string name)
    {
        switch (name)
        {
            case "save":
                if (string.IsNullOrEmpty(pathIF.text) ||
                    string.IsNullOrEmpty(dataIF.text)) return;
                DatabaseReference saveRef = FirebaseDatabase.Instance.GetReference(pathIF.text);
                saveRef.SetValueAsync(dataIF.text, 10, null);
                break;
            case "push":
                if (string.IsNullOrEmpty(pathIF.text) ||
                    string.IsNullOrEmpty(dataIF.text)) return;
                DatabaseReference pushRef = FirebaseDatabase.Instance.GetReference(pathIF.text);
                pushRef.Push(dataIF.text, 10, (null));
                break;
            case "listen":
                if (string.IsNullOrEmpty(pathIF.text)) return;
                DatabaseReference dbref = FirebaseDatabase.Instance.GetReference(pathIF.text);
                dbref.ValueChanged += (sender, e) =>
                {
                    GameObject GO = null;
                    foreach (Transform t in contentItem.transform.parent)
                    {
                        if (t.Find("Path Text").GetComponent<Text>().text == pathIF.text)
                        {
                            GO = t.gameObject;
                        }
                    }
                    if (GO == null)
                        GO = Instantiate(contentItem, contentItem.transform.parent);
                    GO.transform.Find("Path Text").GetComponent<Text>().text = pathIF.text;
                    GO.transform.Find("Data Text").GetComponent<Text>().text = "";
                    Debug.Log(e.Snapshot.GetRawJsonValue());
                    GO.transform.Find("Data Text").GetComponent<Text>().text = e.Snapshot.GetRawJsonValue();
                };
                break;
            case "delete":
                DatabaseReference deleteref = FirebaseDatabase.Instance.GetReference(pathIF.text);
                deleteref.RemoveValueAsync(5, (e) =>
                {
                    if (e.success)
                    {
                        foreach (Transform t in contentItem.transform.parent)
                        {
                            if (t.Find("Path Text").GetComponent<Text>().text == deleteref.Reference)
                                Destroy(t.gameObject);
                        }
                    }
                });
                break;
            case "menu1":
                container1.SetActive(true);
                container2.SetActive(false);
                break;
            case "menu2":
                container1.SetActive(false);
                container2.SetActive(true);
                break;
            case "query":
                Query query = FirebaseDatabase.Instance.GetReference(queryPathIF.text);
                if (orderByDropDown.value > 0)
                {
                    switch (orderByDropDown.value)
                    {
                        case 1:
                            query = query.OrderByKey();
                            break;
                        case 2:
                            query = query.OrderByValue();
                            break;
                        case 3:
                            if (!string.IsNullOrEmpty(orderByIF.text))
                            {
                                query = query.OrderByChild(orderByIF.text);
                            }
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(startAtIF.text))
                {
                    query = query.StartAt(startAtIF.text);
                }
                if (!string.IsNullOrEmpty(endAtIF.text))
                {
                    query = query.EndAt(endAtIF.text);
                }
                if (!string.IsNullOrEmpty(equalToIF.text))
                {
                    query = query.EqualTo(equalToIF.text);
                }
                int limitToFirst;
                if (int.TryParse(limitToFirstIF.text, out limitToFirst))
                {
                    query = query.LimitToFirst(limitToFirst);
                }
                int limitToLast;
                if (int.TryParse(limitToLastIF.text, out limitToLast))
                {
                    query = query.LimitToLast(limitToLast);
                }
                query.GetValueAsync(10, (res) =>
                {
                    if (res.success)
                    {
                        resultText.text = res.data.GetRawJsonValue();
                    }
                    else
                    {
                        resultText.text = res.message;
                    }
                });
                break;
        }
    }
}
