using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SimpleReorderableList{}

[System.Serializable]
public class ReorderableList_Vector3 : ReorderableList<Vector3>{}
[System.Serializable]
public class ReorderableList_RectOffset : ReorderableList<RectOffset>{}

public class ReorderableList<T> : SimpleReorderableList
{
	public List<T> List;
}