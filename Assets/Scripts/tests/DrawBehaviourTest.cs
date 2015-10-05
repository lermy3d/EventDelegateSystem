using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UIEventDelegate;

public class DrawBehaviourTest : MonoBehaviour
{
	public DelegateHolder TopMostClass;
	
	void OnEnable()
	{
		if(TopMostClass.CustomDelegates.Count > 0)
			EventDelegate.Execute(TopMostClass.CustomDelegates);
	}
}

[System.Serializable]
public class DelegateHolder
{
	public string name;

	[Header("Custom Events")]
	public List<EventDelegate> CustomDelegates;
}
