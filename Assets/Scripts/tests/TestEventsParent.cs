using UnityEngine;
using System.Collections;

public enum FireType
{
    Blast,
    AreaDamage,
    Pierce
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
