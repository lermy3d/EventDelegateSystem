using UnityEngine;
using UIEventDelegate;

public class IsolatedTest : MonoBehaviour
{
    public ReorderableEventList ReorderableList;

    void OnEnable()
    {
        if (ReorderableList.List.Count > 0)
            EventDelegate.Execute(ReorderableList.List);
    }
}
