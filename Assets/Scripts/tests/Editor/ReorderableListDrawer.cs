using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;


[CustomPropertyDrawer(typeof(SimpleReorderableList), true)]
public class ReorderableListDrawer : UnityEditor.PropertyDrawer
{
	private UnityEditorInternal.ReorderableList list;
	
	
	private UnityEditorInternal.ReorderableList getList(SerializedProperty property)
	{
		if (list == null)
		{
			list = new ReorderableList(property.serializedObject, property, true, true, true, true);
			list.drawElementCallback = (UnityEngine.Rect rect, int index, bool isActive, bool isFocused) =>
			{
				int indent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = index;
				
				rect.width -= 20;
				rect.x += 4;
				EditorGUI.PropertyField(rect, property.GetArrayElementAtIndex(index), true);
				
				EditorGUI.indentLevel = indent;
			};
		}
		return list;
	}
	
	
	public override float GetPropertyHeight(SerializedProperty property, UnityEngine.GUIContent label)
	{
		return getList(property.FindPropertyRelative("List")).GetHeight();
	}
	
	
	public override void OnGUI(UnityEngine.Rect position, SerializedProperty property, UnityEngine.GUIContent label)
	{
		var listProperty = property.FindPropertyRelative("List");
		var list = getList(listProperty);
		
		var height = 0f;
		for(var i = 0; i < listProperty.arraySize; i++)
		{
			height = Mathf.Max(height, EditorGUI.GetPropertyHeight(listProperty.GetArrayElementAtIndex(i)));
		}
		list.elementHeight = height;
		list.DoList(position);
	}
}