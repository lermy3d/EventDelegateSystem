using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

/// <summary>
/// Draws a single event delegate. Contributed by Lermy Garcia and Adam Byrd.
/// </summary>

[CustomPropertyDrawer(typeof(EDFlagAttribute))]
public class FlagDrawer : PropertyDrawer
{
	public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
	{
		string[] names;
		string propName = prop.name;

		names = prop.enumNames;

		Undo.RecordObject (prop.serializedObject.targetObject, "FlagAttribute Selection");

		EditorGUI.BeginProperty (rect, label, prop);
		prop.intValue = EditorGUI.MaskField(rect, new GUIContent(propName), prop.intValue, names);
		EditorGUI.EndProperty();
	}
}
