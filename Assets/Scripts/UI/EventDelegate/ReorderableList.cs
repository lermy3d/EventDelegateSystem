using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using UIEventDelegate;

public class SimpleReorderableList{}

public class ReorderableList<T> : SimpleReorderableList
{
	public List<T> List;
}

[System.Serializable]
public class ReorderableEventList : ReorderableList<EventDelegate>
{
}