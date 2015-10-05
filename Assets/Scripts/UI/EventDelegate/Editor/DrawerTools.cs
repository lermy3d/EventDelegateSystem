using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

using UIEventDelegate;

public static class DrawerTools
{

    /// <summary>
    /// Helper function that draws a serialized property.
    /// </summary>
    
    static public SerializedProperty DrawProperty (SerializedObject serializedObject, string property, params GUILayoutOption[] options)
    {
        return DrawProperty(null, serializedObject, property, false, options);
    }
    
    /// <summary>
    /// Helper function that draws a serialized property.
    /// </summary>
    
    static public SerializedProperty DrawProperty (string label, SerializedObject serializedObject, string property, params GUILayoutOption[] options)
    {
        return DrawProperty(label, serializedObject, property, false, options);
    }
    
    /// <summary>
    /// Helper function that draws a serialized property.
    /// </summary>
    
    static public SerializedProperty DrawPaddedProperty (SerializedObject serializedObject, string property, params GUILayoutOption[] options)
    {
        return DrawProperty(null, serializedObject, property, true, options);
    }
    
    /// <summary>
    /// Helper function that draws a serialized property.
    /// </summary>
    
    static public SerializedProperty DrawPaddedProperty (string label, SerializedObject serializedObject, string property, params GUILayoutOption[] options)
    {
        return DrawProperty(label, serializedObject, property, true, options);
    }
    
    /// <summary>
    /// Helper function that draws a serialized property.
    /// </summary>
    
    static public SerializedProperty DrawProperty (string label, SerializedObject serializedObject, string property, bool padding, params GUILayoutOption[] options)
    {
        SerializedProperty sp = serializedObject.FindProperty(property);
        
        if (sp != null)
        {
            if (padding) EditorGUILayout.BeginHorizontal();
            
            if (label != null) EditorGUILayout.PropertyField(sp, new GUIContent(label), options);
            else EditorGUILayout.PropertyField(sp, options);
            
            if (padding) 
            {
                DrawPadding();
                EditorGUILayout.EndHorizontal();
            }
        }
        return sp;
    }
    
    /// <summary>
    /// Helper function that draws a serialized property.
    /// </summary>
    
    static public void DrawProperty (string label, SerializedProperty sp, params GUILayoutOption[] options)
    {
        DrawProperty(label, sp, true, options);
    }
    
    /// <summary>
    /// Helper function that draws a serialized property.
    /// </summary>
    
    static public void DrawProperty (string label, SerializedProperty sp, bool padding, params GUILayoutOption[] options)
    {
        if (sp != null)
        {
            if (padding) EditorGUILayout.BeginHorizontal();
            
            if (label != null) EditorGUILayout.PropertyField(sp, new GUIContent(label), options);
            else EditorGUILayout.PropertyField(sp, options);
            
            if (padding)
            {
                DrawPadding();
                EditorGUILayout.EndHorizontal();
            }
        }
    }
    
    /// <summary>
    /// Helper function that draws a compact Vector4.
    /// </summary>
    
    static public void DrawBorderProperty (string name, SerializedObject serializedObject, string field)
    {
        if (serializedObject.FindProperty(field) != null)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(name, GUILayout.Width(75f));
                
                EditorGUIUtility.labelWidth = 50f;
                GUILayout.BeginVertical();
                DrawProperty("Left", serializedObject, field + ".x", GUILayout.MinWidth(80f));
                DrawProperty("Bottom", serializedObject, field + ".y", GUILayout.MinWidth(80f));
                GUILayout.EndVertical();
                
                GUILayout.BeginVertical();
                DrawProperty("Right", serializedObject, field + ".z", GUILayout.MinWidth(80f));
                DrawProperty("Top", serializedObject, field + ".w", GUILayout.MinWidth(80f));
                GUILayout.EndVertical();
                
                EditorGUIUtility.labelWidth = 80f;
            }
            GUILayout.EndHorizontal();
        }
    }
    
    static public void DrawPadding ()
    {
//        GUILayout.Space(18f);
    }
    
    static public List<SerializedProperty> GetListFromPropArray(SerializedProperty arrayProp)
    {
        List<SerializedProperty> list = new List<SerializedProperty>();
        
        if(arrayProp == null || arrayProp.isArray == false)
            return list;
        
        for(int i = 0; i < arrayProp.arraySize; i++)
            list.Add(arrayProp.GetArrayElementAtIndex(i));
        
        return list;
    }
}
