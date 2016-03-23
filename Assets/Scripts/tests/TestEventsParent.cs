using UnityEngine;
using System;

public enum FireType
{
    Blast,
    AreaDamage,
    Pierce
}

[Flags]
public enum MultiFireType
{
    Blast       = 1,
    AreaDamage  = 2,
    Pierce      = 4
}

public class TestEventsParent : MonoBehaviour
{
	public void ParentMessage(string logMessage)
    {
        Debug.Log(logMessage);
	}
    
//    public void ParentMessage()
//    {
//        Debug.Log("overload parent method executed");
//    }
}
