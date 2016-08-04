using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;

using UIEventDelegate;

/// <summary>
/// Draws a single event delegate. Contributed by Lermy Garcia and Adam Byrd.
/// </summary>

[CustomPropertyDrawer(typeof(EventDelegate))]
public class EventDelegateDrawer : PropertyDrawer
{
    EventDelegate eventDelegate = new EventDelegate();

    /// <summary>
    /// The standard height size for each property line.
    /// </summary>
    
    const int lineHeight = 16;
	
    /// <summary>
    /// If you want the property drawer to limit its selection list to values of specified type, set this to something other than 'void'.
    /// </summary>
	
    static public Type filter = typeof(void);
	
    /// <summary>
    /// Whether it's possible to convert between basic types, such as int to string.
    /// </summary>
	
    static public bool canConvert = true;
    
    //vector 4 workaround optimization
	float[] vec4Values;	
	GUIContent[] vec4GUIContent;

    /// <summary>
    /// Rect used to check for the minimalist method.
    /// </summary>
    Rect lineRect;

    /// <summary>
    /// Width value to start using minimalistic method.
    /// </summary>
    //int minimalistWidth = 0; //TODO: implement minimalistic method, unity activates it at 257

    public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
    {
        float yOffset = 0;
        SerializedProperty yOffsetProp = prop.FindPropertyRelative("mYOffset");
        if (yOffsetProp != null)
            yOffset = yOffsetProp.floatValue;

        SerializedProperty showGroup = prop.FindPropertyRelative("mShowGroup");
        if (!showGroup.boolValue)
            return lineHeight + yOffset;

        float lines = (3 * lineHeight) + yOffset;

        SerializedProperty targetProp = prop.FindPropertyRelative("mTarget");
        if (targetProp.objectReferenceValue == null)
            return lines;

        lines += lineHeight;

        SerializedProperty methodProp = prop.FindPropertyRelative("mMethodName");
        
        if (methodProp.stringValue == "<Choose>" || methodProp.stringValue.StartsWith("<Missing - "))
            return lines;

        eventDelegate.target = targetProp.objectReferenceValue as MonoBehaviour;
        eventDelegate.methodName = methodProp.stringValue;
        
        if (eventDelegate.isValid == false)
            return lines;

        SerializedProperty paramArrayProp = prop.FindPropertyRelative("mParameters");
        EventDelegate.Parameter[] ps = eventDelegate.parameters;

        if (ps != null)
        {
            paramArrayProp.arraySize = ps.Length;
            for (int i = 0; i < ps.Length; i++)
            {
                EventDelegate.Parameter param = ps [i];

                lines += lineHeight;

                SerializedProperty paramProp = paramArrayProp.GetArrayElementAtIndex(i);
                SerializedProperty objProp = paramProp.FindPropertyRelative("obj");

                bool useManualValue = paramProp.FindPropertyRelative("paramRefType").enumValueIndex == (int)ParameterType.Value;

                if (useManualValue)
                {
                    if (param.expectedType == typeof(string) || param.expectedType == typeof(int) ||
                        param.expectedType == typeof(float) || param.expectedType == typeof(double) ||
                        param.expectedType == typeof(bool) || param.expectedType.IsEnum ||
                        param.expectedType == typeof(Color))
                    {
                        continue;
                    }
                    else if (param.expectedType == typeof(Vector2) || param.expectedType == typeof(Vector3) || param.expectedType == typeof(Vector4))
                    {
                        //if (lineRect.width < minimalistWidth) //use minimalist method
                        //{
                        //    if (param.expectedType == typeof(Vector2) || param.expectedType == typeof(Vector3))
                        //    {
                        //        lines += lineHeight;
                        //    }
                        //}
                        lines += 4;
                        continue;
                    }
                }
                
                UnityEngine.Object obj = objProp.objectReferenceValue;
                
                if (obj == null)
                    continue;

                System.Type type = obj.GetType();
				
                GameObject selGO = null;
                if (type == typeof(GameObject))
                    selGO = obj as GameObject;
                else if (type.IsSubclassOf(typeof(Component)))
                    selGO = (obj as Component).gameObject;
				
                if (selGO != null)
                    lines += lineHeight;
            }
        }

        return lines;
    }

    public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
    {
        Undo.RecordObject(prop.serializedObject.targetObject, "Delegate Selection");
        
        EditorGUI.BeginProperty(rect, label, prop);
        int indent = EditorGUI.indentLevel;
        
        SerializedProperty showGroup = prop.FindPropertyRelative("mShowGroup");

        SerializedProperty nameProp = prop.FindPropertyRelative("mEventName");
        SerializedProperty targetProp = prop.FindPropertyRelative("mTarget");
        SerializedProperty methodProp = prop.FindPropertyRelative("mMethodName");

        SerializedProperty updateMethodsProp = prop.FindPropertyRelative("mUpdateEntryList");
        
        if (EditorApplication.isCompiling)
            updateMethodsProp.boolValue = true;

        string eventName = nameProp.stringValue;
        MonoBehaviour target = targetProp.objectReferenceValue as MonoBehaviour;

        //controls
        Rect groupPos = new Rect(rect.x, rect.y, rect.width, lineHeight);
        showGroup.boolValue = EditorGUI.Foldout(groupPos, showGroup.boolValue, label);

        if (showGroup.boolValue)
        {
            EditorGUI.indentLevel++;

            //this offset is used to fix the properties overlap in reorderable list per item
            float yOffset = 0;
            SerializedProperty yOffsetProp = prop.FindPropertyRelative("mYOffset");
            if (yOffsetProp != null)
                yOffset = yOffsetProp.floatValue;

            lineRect = rect;
            lineRect.yMin = rect.yMin + lineHeight + yOffset;
            lineRect.yMax = lineRect.yMin + lineHeight;
            
            eventName = EditorGUI.TextField(lineRect, eventName);
            nameProp.stringValue = eventName;
            
            lineRect.yMin += lineHeight;
            lineRect.yMax += lineHeight;
    		
            target = EditorGUI.ObjectField(lineRect, "Notify", target, typeof(MonoBehaviour), true) as MonoBehaviour;
            
            //update method list if target component was modified
            if (targetProp.objectReferenceValue != target)
                updateMethodsProp.boolValue = true;

            targetProp.objectReferenceValue = target;

            if (target != null && target.gameObject != null)
            {
                GameObject go = target.gameObject;
                List<Entry> listWithParams = null;
                
                SerializedProperty entryArrayProp = prop.FindPropertyRelative("mEntryList");

                if (updateMethodsProp.boolValue && EditorApplication.isCompiling == false)
                    UpdateMethods(listWithParams, entryArrayProp, updateMethodsProp, go);
                else
                {
                    //get list from array
                    listWithParams = new List<Entry>();
                    SerializedProperty entryItem;

                    for (int i = 0; i < entryArrayProp.arraySize; i++)
                    {
                        entryItem = entryArrayProp.GetArrayElementAtIndex(i);
                        Component targetComp = entryItem.FindPropertyRelative("target").objectReferenceValue as Component;
                        string name = entryItem.FindPropertyRelative("name").stringValue;
                        
                        listWithParams.Add(new Entry(targetComp, name));
                    }
                }
    
                int index = 0;
                int choice = 0;
                
                string methodName = methodProp.stringValue;
                
                //check and trim missing method message here
                if (methodName.StartsWith("<Missing - ") == true)
                {
                    methodName = methodName.Replace("<Missing - ", "");
                    methodName = methodName.Replace(">", "");
                }

                string[] names = GetNames(listWithParams, methodName, true, out index, methodProp);

                lineRect.yMin += lineHeight;
                lineRect.yMax += lineHeight;
                choice = EditorGUI.Popup(lineRect, "Method", index, names);
    
                //saving selected method
                if (choice > 0) //&& choice != index
                {
                    Entry entry = listWithParams [choice - 1];
    				
                    if(target != entry.target)
                    {
                        target = entry.target as MonoBehaviour;
                        targetProp.objectReferenceValue = target;

                        SerializedProperty cacheProp = prop.FindPropertyRelative("mCached");
                        cacheProp.boolValue = false;
                    }
                    
                    methodName = entry.name;
                    
                    //remove params
                    if (string.IsNullOrEmpty(methodName) == false && methodName.Contains(" ("))
                        methodName = methodName.Remove(methodName.IndexOf(" ("));
                    
                    if(methodName != methodProp.stringValue)
                    {
                        methodProp.stringValue = methodName;
                        entry.name = methodName;

                        SerializedProperty cacheProp = prop.FindPropertyRelative("mCached");
                        cacheProp.boolValue = false;
                    }
                }

                eventDelegate.target = target;
                eventDelegate.methodName = methodName;
                
                if (eventDelegate.isValid == false)
                {
                    if (methodName.StartsWith("<Missing - ") == false)
                        methodName = "<Missing - " + methodName + ">";
                    
                    methodProp.stringValue = methodName;
                
                    EditorGUI.indentLevel = indent;
                    EditorGUI.EndProperty();
                    return;
                }

                //showing parameters
                SerializedProperty paramArrayProp = prop.FindPropertyRelative("mParameters");
                EventDelegate.Parameter[] ps = eventDelegate.parameters;

                if (ps != null)
                {
                    bool showGameObject = false;

                    EditorGUI.indentLevel++;

                    float paramTypeWidth = 100;
                    float lineOriginalMax = lineRect.xMax;
                    lineRect.xMax -= 68;

                    paramArrayProp.arraySize = ps.Length;
                    for (int i = 0; i < ps.Length; i++)
                    {
                        EventDelegate.Parameter param = ps [i];
                        SerializedProperty paramProp = paramArrayProp.GetArrayElementAtIndex(i);
    					
                        SerializedProperty objProp = paramProp.FindPropertyRelative("obj");
                        SerializedProperty fieldProp = paramProp.FindPropertyRelative("field");

                        param.obj = objProp.objectReferenceValue;
                        param.field = fieldProp.stringValue;

                        lineRect.yMin += lineHeight;
                        lineRect.yMax += lineHeight;

                        //showing param info
                        string paramDesc = GetSimpleName(param.expectedType);
                        paramDesc += " " + param.name;

                        if (IsPrimitiveType(param.expectedType))
                        {
                            if(lineOriginalMax == lineRect.xMax)
                                lineRect.xMax -= 68;

                            //only do this if parameter is a primitive type
                            Rect paramTypeRect = new Rect(lineRect.x + lineRect.width - 28, lineRect.y, paramTypeWidth, lineHeight);

                            SerializedProperty paramTypeProp = paramProp.FindPropertyRelative("paramRefType");

                            //draw param type option
                            EditorGUI.PropertyField(paramTypeRect, paramTypeProp, GUIContent.none);
                            param.paramRefType = (ParameterType)paramTypeProp.enumValueIndex;
                        }
                        else
                        {
                            lineRect.xMax = lineOriginalMax;
                        }

                        bool useManualValue = paramProp.FindPropertyRelative("paramRefType").enumValueIndex == (int)ParameterType.Value;

                        if (useManualValue)
                        {
                            if (param.expectedType == typeof(string))
                            {
                                SerializedProperty valueProp = paramProp.FindPropertyRelative("argStringValue");
                                EditorGUI.PropertyField(lineRect, valueProp, new GUIContent(paramDesc));

                                param.value = valueProp.stringValue;
                            }
                            else if (param.expectedType == typeof(int))
                            {
                                SerializedProperty valueProp = paramProp.FindPropertyRelative("argIntValue");
                                EditorGUI.PropertyField(lineRect, valueProp, new GUIContent(paramDesc));

                                param.value = valueProp.intValue;
                            }
                            else if (param.expectedType == typeof(float))
                            {
                                SerializedProperty valueProp = paramProp.FindPropertyRelative("argFloatValue");
                                EditorGUI.PropertyField(lineRect, valueProp, new GUIContent(paramDesc));

                                param.value = valueProp.floatValue;
                            }
                            else if (param.expectedType == typeof(double))
                            {
                                SerializedProperty valueProp = paramProp.FindPropertyRelative("argDoubleValue");
                                EditorGUI.PropertyField(lineRect, valueProp, new GUIContent(paramDesc));

                                param.value = valueProp.doubleValue;
                            }
                            else if (param.expectedType == typeof(bool))
                            {
                                SerializedProperty valueProp = paramProp.FindPropertyRelative("argBoolValue");
                                EditorGUI.PropertyField(lineRect, valueProp, new GUIContent(paramDesc));

                                param.value = valueProp.boolValue;
                            }
                            else if (param.expectedType == typeof(Color))
                            {
                                SerializedProperty valueProp = paramProp.FindPropertyRelative("argColor");
                                EditorGUI.PropertyField(lineRect, valueProp, new GUIContent(paramDesc));

                                param.value = valueProp.colorValue;
                            }
                            else if (param.expectedType == typeof(Vector2))
                            {
                                SerializedProperty valueProp = paramProp.FindPropertyRelative("argVector2");
                                lineRect.y += 2;

                                EditorGUI.PropertyField(lineRect, valueProp, new GUIContent(paramDesc));

                                param.value = valueProp.vector2Value;
                            }
                            else if (param.expectedType == typeof(Vector3))
                            {
                                SerializedProperty valueProp = paramProp.FindPropertyRelative("argVector3");
                                lineRect.y += 2;

                                EditorGUI.PropertyField(lineRect, valueProp, new GUIContent(paramDesc));

                                param.value = valueProp.vector3Value;
                            }
                            else if (param.expectedType == typeof(Vector4))
                            {
                                SerializedProperty valueProp = paramProp.FindPropertyRelative("argVector4");
                                Vector4 vec4 = valueProp.vector4Value;

                                lineRect.y += 2;

                                //workaround for vector 4, it uses an extra line.
                                //valueProp.vector4Value = EditorGUI.Vector4Field(lineRect, paramDesc, valueProp.vector4Value);

                                //create all this values just once
                                if (vec4Values == null)
                                    vec4Values = new float[4];

                                vec4Values[0] = vec4.x;
                                vec4Values[1] = vec4.y;
                                vec4Values[2] = vec4.z;
                                vec4Values[3] = vec4.w;

                                if (vec4GUIContent == null)
                                    vec4GUIContent = new GUIContent[4];

                                vec4GUIContent[0] = new GUIContent("X");
                                vec4GUIContent[1] = new GUIContent("Y");
                                vec4GUIContent[2] = new GUIContent("Z");
                                vec4GUIContent[3] = new GUIContent("W");

                                EditorGUI.LabelField(lineRect, paramDesc);

                                Rect vector4Line = new Rect(lineRect);
                                vector4Line.xMin += (EditorGUI.indentLevel * lineHeight) + 86;

                                EditorGUI.MultiFloatField(vector4Line, vec4GUIContent, vec4Values);

                                valueProp.vector4Value = new Vector4(vec4Values[0], vec4Values[1], vec4Values[2], vec4Values[3]);
                                param.value = valueProp.vector4Value;
                            }
                            else if (param.expectedType.IsEnum)
                            {
                                SerializedProperty valueProp = paramProp.FindPropertyRelative("argIntValue");

                                if (param.expectedType.GetAttribute<FlagsAttribute>() != null)
                                {
                                    param.value = EditorGUI.MaskField(lineRect, new GUIContent(paramDesc), valueProp.intValue, Enum.GetNames(param.expectedType));
                                }
                                else
                                {
                                    Enum selectedOpt = (Enum)Enum.ToObject(param.expectedType, valueProp.intValue);
                                    param.value = EditorGUI.EnumPopup(lineRect, new GUIContent(paramDesc), selectedOpt);
                                }

                                valueProp.intValue = (int)param.value;
                            }
                            else
                            {
                                showGameObject = true;
                            }
                        }

                        if(showGameObject || !useManualValue)
                        {
                            UnityEngine.Object obj = param.obj;

                            obj = EditorGUI.ObjectField(lineRect, paramDesc, obj, typeof(UnityEngine.Object), true);

                            param.obj = obj;
                            objProp.objectReferenceValue = obj;

                            if (obj == null)
                                continue;

                            //show gameobject
                            GameObject selGO = null;
                            System.Type type = param.obj.GetType();
                            if (type == typeof(GameObject))
                                selGO = param.obj as GameObject;
                            else if (type.IsSubclassOf(typeof(Component)))
                                selGO = (param.obj as Component).gameObject;

                            if (selGO != null)
                            {
                                // Parameters must be exact -- they can't be converted like property bindings
                                filter = param.expectedType;
                                canConvert = false;
                                List<Entry> ents = GetProperties(selGO, true, false);

                                int selection;
                                string[] props = GetNames(ents, EventDelegate.GetFuncName(param.obj, param.field), false, out selection);

                                lineRect.yMin += lineHeight;
                                lineRect.yMax += lineHeight;
                                int newSel = EditorGUI.Popup(lineRect, " ", selection, props);

                                if (newSel != selection)
                                {
                                    if (newSel == 0)
                                    {
                                        param.obj = selGO;
                                        param.field = null;

                                        objProp.objectReferenceValue = selGO;
                                        fieldProp.stringValue = null;
                                    }
                                    else
                                    {
                                        param.obj = ents[newSel - 1].target;
                                        param.field = ents[newSel - 1].name;

                                        objProp.objectReferenceValue = param.obj;
                                        fieldProp.stringValue = param.field;
                                    }
                                }
                            }
                            else if (!string.IsNullOrEmpty(param.field))
                                param.field = null;

                            filter = typeof(void);
                            canConvert = true;
                        }

                        showGameObject = false;
                    }
                }
            }
        }
        
        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
    
    /// <summary>
    /// Updates the methods list and property array cache from the selected GameObject.
    /// </summary>

    void UpdateMethods(List<Entry> listWithParams, SerializedProperty entryArrayProp, SerializedProperty updateMethodsProp, GameObject go)
    {
        listWithParams = GetMethods(go, true);
        entryArrayProp.ClearArray();
        SerializedProperty entryProp;
        
        //update serialized entries
        foreach (Entry entryItem in listWithParams)
        {
            if (entryArrayProp.arraySize == 0)
            {
                entryArrayProp.InsertArrayElementAtIndex(0);
                entryProp = entryArrayProp.GetArrayElementAtIndex(0);
            } else
            {
                entryArrayProp.InsertArrayElementAtIndex(entryArrayProp.arraySize - 1);
                entryProp = entryArrayProp.GetArrayElementAtIndex(entryArrayProp.arraySize - 1);
            }
            
            entryProp.FindPropertyRelative("target").objectReferenceValue = entryItem.target;
            entryProp.FindPropertyRelative("name").stringValue = entryItem.name;
        }
        
        updateMethodsProp.boolValue = false;
    }
    
    /// <summary>
    /// Convert the specified list of delegate entries into a string array.
    /// </summary>
    
    static public string[] GetNames(List<Entry> list, string choice, bool inlcudeParams, out int index, SerializedProperty methodProp = null)
    {
        index = 0;

        if (list == null)
            return new string[0];
        
        string[] names = new string[list.Count + 1];
        names [0] = "<Choose>";
        
        for (int i = 0; i < list.Count;)
        {
            Entry entry = list [i];
            
            //check if comes with params and remove
            string del = entry.name;
            string methodName = "";
            
            if (string.IsNullOrEmpty(del) == false && del.Contains(" ("))
                methodName = del.Remove(del.IndexOf(" ("));
            
            del = EventDelegate.GetFuncName(entry.target, del);
            
            if (inlcudeParams)
                names [++i] = EventDelegate.GetFuncName(entry.target, entry.name);
            else
                names [++i] = del;
            
            if (index == 0)
            {
                if (choice == methodName)
                    index = i;
                else if (string.Equals(del, choice))
                    index = i;
            }
        }
        
        if (index != 0)
        {
            if (names [index].Contains(" ("))
                names [index] = names [index].Remove(names [index].IndexOf(" ("));
            
        } else if (methodProp != null && methodProp.stringValue.StartsWith("<Missing - "))
            names [0] = methodProp.stringValue;
        
        return names;
    }
    
    /// <summary>
    /// Returns only the name from a given type.
    /// </summary>
    
    static public string GetSimpleName(System.Type type)
    {
        if (type == null)
            return "";
        
        string name = type.ToString();
        
        return name.Substring(name.LastIndexOf('.') + 1);
    }

    static public bool IsPrimitiveType(Type expectedType)
    {
        if(expectedType == typeof(string) || expectedType == typeof(int) || expectedType == typeof(float) || expectedType == typeof(double) ||
            expectedType == typeof(bool) || expectedType == typeof(Vector2) || expectedType == typeof(Vector3) || expectedType == typeof(Vector4) ||
            expectedType == typeof(Color) || expectedType.IsEnum)
        {
            return true;
        }

        return false;
    }
    
    #if REFLECTION_SUPPORT
    
    /// <summary>
    /// Whether we can assign the property using the specified value.
    /// </summary>
    
    bool Convert (ref object value)
    {
        if (mTarget == null) return false;
        
        Type to = GetPropertyType();
        Type from;
        
        if (value == null)
        {
            #if NETFX_CORE
            if (!to.GetTypeInfo().IsClass) return false;
            #else
            if (!to.IsClass) return false;
            #endif
            from = to;
        }
        else from = value.GetType();
        return Convert(ref value, from, to);
    }
    #else // Everything below = no reflection support
    bool Convert(ref object value)
    {
        return false;
    }
    #endif
    
    /// <summary>
    /// Whether we can convert one type to another for assignment purposes.
    /// </summary>
    
    static public bool Convert(Type from, Type to)
    {
        object temp = null;
        return Convert(ref temp, from, to);
    }
    
    /// <summary>
    /// Whether we can convert one type to another for assignment purposes.
    /// </summary>
    
    static public bool Convert(object value, Type to)
    {
        if (value == null)
        {
            value = null;
            return Convert(ref value, to, to);
        }
        return Convert(ref value, value.GetType(), to);
    }
    
    /// <summary>
    /// Whether we can convert one type to another for assignment purposes.
    /// </summary>
    
    static public bool Convert(ref object value, Type from, Type to)
    {
        #if REFLECTION_SUPPORT
        // If the value can be assigned as-is, we're done
        #if NETFX_CORE
        if (to.GetTypeInfo().IsAssignableFrom(from.GetTypeInfo())) return true;
        #else
        if (to.IsAssignableFrom(from)) return true;
        #endif
        
        #else
        if (from == to)
            return true;
        #endif
        // If the target type is a string, just convert the value
        if (to == typeof(string))
        {
            value = (value != null) ? value.ToString() : "null";
            return true;
        }
        
        // If the value is null we should not proceed further
        if (value == null)
            return false;
        
        if (to == typeof(int))
        {
            if (from == typeof(string))
            {
                int val;
                
                if (int.TryParse((string)value, out val))
                {
                    value = val;
                    return true;
                }
            } else if (from == typeof(float))
            {
                value = Mathf.RoundToInt((float)value);
                return true;
            }
        } else if (to == typeof(float))
        {
            if (from == typeof(string))
            {
                float val;
                
                if (float.TryParse((string)value, out val))
                {
                    value = val;
                    return true;
                }
            }
        }
        return false;
    }
    
    /// <summary>
    /// Collect a list of usable delegates from the specified target game object.
    /// </summary>
    
    static public List<Entry> GetMethods(GameObject target, bool includeParams = false)
    {
        List<Entry> list = new List<Entry>();
        
        if (target == null)
            return list;
        
        MonoBehaviour[] comps = target.GetComponents<MonoBehaviour>();
        
        for (int i = 0, imax = comps.Length; i < imax; ++i)
        {
            MonoBehaviour mb = comps [i];
            if (mb == null)
                continue;
            
            MethodInfo[] methods = mb.GetType().GetMethods(EventDelegate.MethodFlags);
            
            for (int b = 0; b < methods.Length; ++b)
            {
                MethodInfo mi = methods [b];
                
                //filter methods
                string name = mi.Name;
                
                if (name == "obj_address")
                    continue;
                if (name == "MemberwiseClone")
                    continue;
                if (name == "Finalize")
                    continue;
                if (name == "Invoke")
                    continue;
                if (name == "InvokeRepeating")
                    continue;
                if (name == "CancelInvoke")
                    continue;
                if (name == "BroadcastMessage")
                    continue;
                if (name == "Equals")
                    continue;
                if (name == "CompareTag")
                    continue;
                if (name == "ToString")
                    continue;
                if (name == "GetType")
                    continue;
                if (name == "GetHashCode")
                    continue;
                if (name == "GetInstanceID")
                    continue;
                if (name.StartsWith("StartCoroutine"))
                    continue;
                if (name.StartsWith("StopCoroutine"))
                    continue;
//                if (name.StartsWith("StopAllCoroutines"))
//                    continue;
                if (name.StartsWith("set_"))
                    continue;
                if (name.StartsWith("get_"))
                    continue;
                if (name.StartsWith("GetComponent"))
                    continue;
                if (name.StartsWith("SendMessage"))
                    continue;
                
                Entry entry = new Entry();
                entry.target = mb;
                entry.name = name;
                
                if (ExistOverloadedMethod(methods, entry))
                    continue;
                
                if (includeParams)
                {
                    ParameterInfo[] parameters = mi.GetParameters();
                    
                    entry.name += " (";
                    if (parameters != null && parameters.Length > 0)
                    {                    
                        int count = 0;
                        foreach (ParameterInfo paramInfo in parameters)
                        {
                            entry.name += GetSimpleName(paramInfo.ParameterType) + " " + paramInfo.Name;
                            count++;
                            
                            //adding for next param
                            if (count < parameters.Length)
                                entry.name += ", ";
                        }
                    }
                    entry.name += ")";
                }
                
                list.Add(entry);
            }
        }
        
        list.Sort();
        
        return list;
    }
    
    /// <summary>
    /// Collect a list of usable properties and fields.
    /// </summary>
	
    static public List<Entry> GetProperties(GameObject target, bool read, bool write)
    {
        Component[] comps = target.GetComponents<Component>();
		
        List<Entry> list = new List<Entry>();
		
        for (int i = 0, imax = comps.Length; i < imax; ++i)
        {
            Component comp = comps [i];
            if (comp == null)
                continue;
			
            Type type = comp.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            FieldInfo[] fields = type.GetFields(flags);
            PropertyInfo[] props = type.GetProperties(flags);
			
            // The component itself without any method
            if (Convert(comp, filter))
            {
                Entry ent = new Entry();
                ent.target = comp;
                
                if (list.Contains(ent) == false)                
                    list.Add(ent);
            }
			
            for (int b = 0; b < fields.Length; ++b)
            {
                FieldInfo field = fields [b];
				
                if (filter != typeof(void))
                {
                    if (canConvert)
                    {
                        if (!Convert(field.FieldType, filter))
                            continue;
                    } else if (!filter.IsAssignableFrom(field.FieldType))
                        continue;
                }
				
                Entry ent = new Entry();
                ent.target = comp;
                ent.name = field.Name;
                list.Add(ent);
            }
			
            for (int b = 0; b < props.Length; ++b)
            {
                PropertyInfo prop = props [b];
                if (read && !prop.CanRead)
                    continue;
                if (write && !prop.CanWrite)
                    continue;
				
                if (filter != typeof(void))
                {
                    if (canConvert)
                    {
                        if (!Convert(prop.PropertyType, filter))
                            continue;
                    } else if (!filter.IsAssignableFrom(prop.PropertyType))
                        continue;
                }
				
                Entry ent = new Entry();
                ent.target = comp;
                ent.name = prop.Name;
                list.Add(ent);
            }
        }
        return list;
    }

    /// <summary>
    /// Returns if an overloaded method exists in an array of functions.
    /// </summary>

    static public bool ExistOverloadedMethod(MethodInfo[] methodArray, Entry entry)
    {
        if (methodArray == null || methodArray.Length <= 0 || entry == null || string.IsNullOrEmpty(entry.name))
            return false;
    
        string method = entry.name;
        
        if (method.Contains(" ("))
            method = method.Remove(method.IndexOf(" ("));
        
        int count = 0;

        foreach (MethodInfo methodInfo in methodArray)
        {
            if (methodInfo.Name.Equals(method))
            { 
                if (count > 0)
                    return true;
                else
                    count++;
            }
        }
        
        return false;
    }
}