using UnityEngine;
using System.Collections;

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
