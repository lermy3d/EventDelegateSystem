using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UIEventDelegate;

[System.Serializable]
public class ReorderableListEventList : ReorderableList<EventDelegate>{}

public class TestExecOrderUnityEvent : MonoBehaviour
{
    [Header("Current Unity events")]
    public UnityEvent OnEnableEvents;
    
    [Header("Reorderable List")]
	public ReorderableListEventList UnityListDrawer;

    [Header("Custom Events")]
    public List<EventDelegate> CustomDelegates;
    
    void OnEnable()
    {
        if (OnEnableEvents != null)
            OnEnableEvents.Invoke();

        if(CustomDelegates.Count > 0)
            EventDelegate.Execute(CustomDelegates);
    }
    
    public void SetParent(Transform child, Transform newParent = null)
    {
        child.SetParent(newParent, false);
    }
    
    public void TestPrimitiveParams(string valString, int valInteger, float valFloat, double valDouble, bool valBool, Transform valTranform)
    {
        Debug.Log("valString: " + valString);
        
        Debug.Log("valInteger: " + valInteger);
        
        Debug.Log("valFloat: " + valFloat);
        
        Debug.Log("valDouble: " + valDouble);
        
        Debug.Log("valBool: " + valBool);
        
        Debug.Log("valTransform: " + valTranform);
    }
}
