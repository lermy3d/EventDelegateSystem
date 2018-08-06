#if UNITY_EDITOR || !UNITY_FLASH
#define REFLECTION_SUPPORT
#endif

#if REFLECTION_SUPPORT
using System.Reflection;
#endif

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UIEventDelegate
{

/// <summary>
/// Parameter type is used to allow switching between reference or manual value setup in inspector for the parameters passed.
/// </summary>
public enum ParameterType
{
    Value,
    Reference
}

/// <summary>
/// Delegate callback that Unity can serialize and set via Inspector.
/// </summary>

[System.Serializable]
public class EventDelegate
{
	/// <summary>
	/// Delegates can have parameters, and this class makes it possible to save references to properties
	/// that can then be passed as function arguments, such as transform.position or widget.color.
	/// </summary>

	[Serializable]
	public class Parameter
	{
		public UnityEngine.Object obj;
		public string field;
        
        public ParameterType paramRefType;

        public string argStringValue;
        public int argIntValue;
        public float argFloatValue;
        public double argDoubleValue;
        public bool argBoolValue;
		public Color argColor;
        public Vector2 argVector2;
		public Vector3 argVector3;
		public Vector4 argVector4;

		public Parameter () { }
        public Parameter (UnityEngine.Object obj, string field) { this.obj = obj; this.field = field; }
		public Parameter (object val) { mValue = val; }

        object mValue;

#if REFLECTION_SUPPORT
		[System.NonSerialized]
		public System.Type expectedType = typeof(void);
        
        [System.NonSerialized] public string name;

		// Cached values
		[System.NonSerialized] public bool cached = false;
		[System.NonSerialized] public PropertyInfo propInfo;
        [System.NonSerialized] public FieldInfo fieldInfo;

        /// <summary>
        /// Return the property's current value.
        /// </summary>

        public object value
		{
			get
			{
				if (mValue != null) return mValue;

				if (!cached)
				{
					cached = true;
					fieldInfo = null;
					propInfo = null;
                    
                    if(paramRefType == ParameterType.Value)
                    {
                        if (expectedType == typeof(string))
                        {
                            mValue = argStringValue;
                            return argStringValue;
                        }
                        else if (expectedType == typeof(int))
                        {
                            mValue = argIntValue;
                            return argIntValue;
                        }
                        else if (expectedType == typeof(float))
                        {
                            mValue = argFloatValue;
                            return argFloatValue;
                        }
                        else if (expectedType == typeof(double))
                        {
                            mValue = argDoubleValue;
                            return argDoubleValue;
                        }
                        else if (expectedType == typeof(bool))
                        {
                            mValue = argBoolValue;
                            return argBoolValue;
                        }
                        else if (expectedType == typeof(Color))
                        {
                            mValue = argColor;
                            return argColor;
                        }
                        else if (expectedType == typeof(Vector2))
                        {
                            mValue = argVector2;
                            return argVector2;
                        }
                        else if (expectedType == typeof(Vector3))
                        {
                            mValue = argVector3;
                            return argVector3;
                        }
                        else if (expectedType == typeof(Vector4))
                        {
                            mValue = argVector4;
                            return argVector4;
                        }
                        else if (expectedType.IsEnum)
                        {
                            mValue = (Enum)Enum.ToObject(expectedType, argIntValue);
                            return mValue;
                        }
                    }
					
					if (obj != null && !string.IsNullOrEmpty(field))
					{
						System.Type type = obj.GetType();
#if NETFX_CORE
						propInfo = type.GetRuntimeProperty(field);
						if (propInfo == null) fieldInfo = type.GetRuntimeField(field);
#else
						propInfo = type.GetProperty(field);
						if (propInfo == null) fieldInfo = type.GetField(field);
#endif
					}
				}
				if (propInfo != null) return propInfo.GetValue(obj, null);
				if (fieldInfo != null) return fieldInfo.GetValue(obj);
				if (obj != null) return obj;
#if !NETFX_CORE
				if (expectedType != null && expectedType.IsValueType) return null;
#endif
				return System.Convert.ChangeType(null, expectedType);
			}
			set
			{
				mValue = value;
                
                if(mValue == null)
                {
                    cached = false;
                }
			}
		}

		/// <summary>
		/// Parameter type -- a convenience function.
		/// </summary>

		public System.Type type
		{
			get
			{
				if (mValue != null) return mValue.GetType();
				if (obj == null) return typeof(void);
				return obj.GetType();
			}
		}
#else // REFLECTION_SUPPORT
		public object value { get { if (mValue != null) return mValue; return obj; } }
 #if UNITY_EDITOR || !UNITY_FLASH
		public System.Type type { get { if (mValue != null) return mValue.GetType(); return typeof(void); } }
 #else
		public System.Type type { get { if (mValue != null) return mValue.GetType(); return null; } }
 #endif
#endif
	}
    
    [SerializeField] public string mEventName = "Event";
    
    [HideInInspector]
    public bool mShowGroup = true;

    [HideInInspector]
    public bool mUpdateEntryList = true;
    
    [HideInInspector]
    public Entry[] mEntryList;
    
    [HideInInspector]
    static public BindingFlags MethodFlags = BindingFlags.OptionalParamBinding | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;

	[HideInInspector]
    static public BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public;

	[SerializeField] UnityEngine.Object mTarget;
	[SerializeField] string mMethodName;
	[SerializeField] Parameter[] mParameters;

    [SerializeField] bool mCached = false;

	/// <summary>
	/// Whether the event delegate will be removed after execution.
	/// </summary>

	public bool oneShot = false;

	//delegate prototypes
    public delegate bool BoolCallback();
    public delegate Behaviour BehaviourCallback();
    public delegate UnityEngine.Object UnityObjectCallback();
    public delegate System.Object SystemObjectCallback();
	public delegate void Callback();
    
    // Private variables
	[System.NonSerialized] Delegate mCachedCallback;
	[System.NonSerialized] FieldInfo mCachedFieldInfo;
	[System.NonSerialized] PropertyInfo mCachedPropertyInfo;

	[System.NonSerialized] bool mRawDelegate = false;
#if REFLECTION_SUPPORT
	[System.NonSerialized] MethodInfo mMethod;
    [System.NonSerialized] ParameterInfo[] mParameterInfos;
	[System.NonSerialized] object[] mArgs;
#endif

	/// <summary>
	/// Event delegate's target object.
	/// </summary>

	public UnityEngine.Object target
	{
		get
		{
			return mTarget;
		}
		set
		{
			mTarget = value;
			mCachedCallback = null;
			mCachedFieldInfo = null;
			mCachedPropertyInfo = null;
			mRawDelegate = false;
			mCached = false;
#if REFLECTION_SUPPORT
			mMethod = null;
			mParameterInfos = null;
#endif
			mParameters = null;
		}
	}

	/// <summary>
	/// Event delegate's method name.
	/// </summary>

	public string methodName
	{
		get
		{
			return mMethodName;
		}
		set
		{
			mMethodName = value;
			mCachedCallback = null;
			mCachedFieldInfo = null;
			mCachedPropertyInfo = null;
			mRawDelegate = false;
			mCached = false;
#if REFLECTION_SUPPORT
			mMethod = null;
			mParameterInfos = null;
#endif
			mParameters = null;
		}
	}

	/// <summary>
	/// Optional parameters if the method requires them.
	/// </summary>

	public Parameter[] parameters
	{
		get
		{
            if (!mCached)
            {
                Cache();
            }

			return mParameters;
		}
	}

	/// <summary>
	/// Whether this delegate's values have been set.
	/// </summary>

	public bool isValid
	{
		get
		{
            if (!mCached)
            {
                Cache();
            }

			return (mRawDelegate && mCachedCallback != null) || (mCachedFieldInfo != null || mCachedPropertyInfo != null)
					|| ExistMethod() || ExistField();
		}
	}

	/// <summary>
	/// Whether the target script is actually enabled.
	/// </summary>

	public bool isEnabled
	{
		get
		{
            if (!mCached)
            {
                Cache();
            }

			if (mRawDelegate && mCachedCallback != null)
                return true;
			if (mTarget == null)
                return false;
			
			Behaviour behaviour = mTarget as Behaviour;
			if (behaviour)
			{
				return behaviour.enabled;
			}
			
			return true;
		}
	}

	public EventDelegate () { }
	public EventDelegate (Delegate call) { Set(call); }
	public EventDelegate (UnityEngine.Object target, string methodName) { Set(target, methodName); }

	/// <summary>
	/// GetMethodName is not supported on some platforms.
	/// </summary>

#if REFLECTION_SUPPORT
 #if !UNITY_EDITOR && NETFX_CORE
    static string GetMethodName (Delegate callback)
	{
        if(callback == null)
            return "";
    
        return callback.GetMethodInfo().Name;
	}

    static bool IsValid (Delegate callback)
	{
        return callback != null && callback.GetMethodInfo() != null;
	}
 #else
	static string GetMethodName (Delegate callback) { return callback.Method.Name; }
    static bool IsValid (Delegate callback) { return callback != null && callback.Method != null; }
 #endif
#else
    static bool IsValid (Delegate callback) { return callback != null; }
#endif

	/// <summary>
	/// Equality operator.
	/// </summary>

	public override bool Equals (object obj)
	{
		if (obj == null) return !isValid;

		if (obj is Callback)
		{
			Callback callback = obj as Callback;
#if REFLECTION_SUPPORT
			if (callback.Equals(mCachedCallback)) return true;
			

			UnityEngine.Object target = callback.Target as UnityEngine.Object;
			return (mTarget == target && string.Equals(mMethodName, GetMethodName(callback)));
#elif UNITY_FLASH
			return (callback == mCachedCallback);
#else
			return callback.Equals(mCachedCallback);
#endif
		}
        
        if (obj is Delegate)
        {
            Delegate callback = obj as Delegate;

            if (callback.Equals(mCachedCallback))
                return true;
           
			UnityEngine.Object target = callback.Target as UnityEngine.Object;
			return (mTarget == target && string.Equals(mMethodName, GetMethodName(callback)));
        }
		
		if (obj is EventDelegate)
		{
			EventDelegate del = obj as EventDelegate;
			return (mTarget == del.mTarget && string.Equals(mMethodName, del.mMethodName));
		}
		return false;
	}

	static int s_Hash = "EventDelegate".GetHashCode();

	/// <summary>
	/// Used in equality operators.
	/// </summary>

	public override int GetHashCode () { return s_Hash; }

	/// <summary>
	/// Set the delegate callback directly.
	/// </summary>

	void Set (Delegate call)
	{
		Clear();

		if (call != null && IsValid(call))
		{
#if REFLECTION_SUPPORT
			mTarget = call.Target as UnityEngine.Object;

			if (mTarget == null)
			{
				mRawDelegate = true;
				mCachedCallback = call;
				mMethodName = null;
			}
			else
			{
				mMethodName = GetMethodName(call);
				mRawDelegate = false;
			}
#else
			mRawDelegate = true;
			mCachedCallback = call;
#endif
		}
	}

	/// <summary>
	/// Set the delegate callback using the target and method names.
	/// </summary>

	public void Set (UnityEngine.Object target, string methodName)
	{
		Clear();
		mTarget = target;
		mMethodName = methodName;
	}
    
    public bool ExistMethod()
    {
        if (mTarget != null && string.IsNullOrEmpty(mMethodName) == false)
        {
            System.Type type = mTarget.GetType();

            MethodInfo methodInfo = null;
            
            try
            {
                while(type != null)
                {
                    methodInfo = type.GetMethod(mMethodName, MethodFlags);
                    if (methodInfo != null)
                        break;
                    
                    type = type.BaseType;
                }
             }
            catch(System.Exception){}
            
            return methodInfo != null;
        }
            
        return false;
    }

    public bool ExistField()
    {
        if (mTarget != null && string.IsNullOrEmpty(mMethodName) == false)
        {
            System.Type type = mTarget.GetType();

            FieldInfo fieldInfo = null;
            PropertyInfo propertyInfo = null;
            
            try
            {
                while(type != null)
                {
                    fieldInfo = type.GetField(mMethodName, FieldFlags);
                    if (fieldInfo != null)
                        break;

                    propertyInfo = type.GetProperty(mMethodName, FieldFlags);
                    if (propertyInfo != null)
                        break;

                    type = type.BaseType;
                }
             }
            catch(System.Exception){}
            
            return fieldInfo != null || propertyInfo != null;
        }
            
        return false;
    }

	/// <summary>
	/// Cache the callback and create the list of the necessary parameters.
	/// </summary>

	void Cache (bool showError = true)
	{
		mCached = true;
		if (mRawDelegate) return;

#if REFLECTION_SUPPORT
		if (mCachedCallback == null || (mCachedCallback.Target as UnityEngine.Object) != mTarget || GetMethodName(mCachedCallback) != mMethodName)
		{
			if (mTarget != null && !string.IsNullOrEmpty(mMethodName))
			{
				System.Type type = mTarget.GetType();
 #if NETFX_CORE
				try
				{
					IEnumerable<MethodInfo> methods = type.GetRuntimeMethods();

					MethodInfo mi = null;
					for (int i = 0, imax = methods.Length; i < imax; ++i, mi = null)
					{
						mi = methods [i];
						if (mi == null)
							continue;

						if (mi.Name == mMethodName)
						{
							mMethod = mi;
							break;
						}
					}
				}
				catch (System.Exception ex)
				{
					Debug.LogError("Failed to bind " + type + "." + mMethodName + "\n" +  ex.Message);
					return;
				}
 #else // NETFX_CORE
				for (mMethod = null; type != null; )
				{
					try
					{
                        mMethod = type.GetMethod(mMethodName, MethodFlags);
						if (mMethod != null)
                            break;
					}
					catch(System.Exception exc)
                    {
                        Debug.LogError(exc.Message + " Inner exception: " +  exc.InnerException);
                    }
  #if UNITY_WP8 || UNITY_WP_8_1
					// For some odd reason Type.GetMethod(name, bindingFlags) doesn't seem to work on WP8...
					try
					{
						mMethod = type.GetMethod(mMethodName);
						if (mMethod != null) break;
					}
					catch (System.Exception) { }
  #endif
					type = type.BaseType;
				}
 #endif // NETFX_CORE

				if (mMethod == null)
				{
					//clearing prev cached values, if ever...
					mArgs = null;
					mParameterInfos = null;
					mCachedCallback = null;

					//check if field or property
					FieldInfo fieldInfo = null;
					PropertyInfo propertyInfo = null;
					
					type = mTarget.GetType();
					try
					{
						while(type != null)
						{
							fieldInfo = type.GetField(mMethodName, FieldFlags);
							if (fieldInfo != null)
								break;
							
							propertyInfo = type.GetProperty(mMethodName, FieldFlags);
							if (propertyInfo != null)
								break;
							
							type = type.BaseType;
						}
					}
					catch(System.Exception){}

					if(fieldInfo != null)
					{
						mCachedFieldInfo = fieldInfo;

						if (mParameters == null || mParameters.Length != 1)
						{
							mParameters = new Parameter[1];
							mParameters[0] = new Parameter();
						}
						
						mParameters[0].expectedType = fieldInfo.FieldType;
						mParameters[0].name = mMethodName;

						return;
					}
					else if(propertyInfo != null)
					{
						mCachedPropertyInfo = propertyInfo;

						if (mParameters == null || mParameters.Length != 1)
						{
							mParameters = new Parameter[1];
							mParameters[0] = new Parameter();
						}

						mParameters[0].expectedType = propertyInfo.PropertyType;
						mParameters[0].name = mMethodName;

						return;
					}
					
					//at this point
                    //method or target was most likely changed
                    
                    if(showError)
					    Debug.LogWarning("Could not find method or field '" + mMethodName + "' on " + mTarget.GetType(), mTarget);

					return;
				}

				// Get the list of expected parameters
				mParameterInfos = mMethod.GetParameters();

                if (mParameterInfos.Length == 0)
				{
					// No parameters means we can create a simple delegate for it, optimizing the call
 #if NETFX_CORE
                    mCachedCallback = Delegate.CreateDelegatem(mMethod.ReturnType, mTarget, mMethodName);
 #else
                    //some UI components (like Button) need this specification for their methods to work
                    if(mMethod.ReturnType == typeof(System.Boolean))
                        mCachedCallback = Delegate.CreateDelegate(typeof(BoolCallback), mTarget, mMethodName);
                    else if(mMethod.ReturnType.IsSubclassOf(typeof(Behaviour)))
                        mCachedCallback = Delegate.CreateDelegate(typeof(BehaviourCallback), mTarget, mMethodName);
                    else if(mMethod.ReturnType.IsSubclassOf(typeof(UnityEngine.Object)))
                        mCachedCallback = Delegate.CreateDelegate(typeof(UnityObjectCallback), mTarget, mMethodName);
                    else if (mMethod.ReturnType != typeof(void) && mMethod.ReturnType.IsSubclassOf(typeof(System.Object)))
                        mCachedCallback = Delegate.CreateDelegate(typeof(SystemObjectCallback), mTarget, mMethodName);
                    else if(mMethod.ReturnType == typeof(void))
                        mCachedCallback = Delegate.CreateDelegate(typeof(Callback), mTarget, mMethodName);
                    else
                        mCachedCallback = Delegate.CreateDelegate(mMethod.ReturnType, mTarget, mMethodName);
 #endif

					mArgs = null;
					mParameters = null;
					return;
				}
				else
                    mCachedCallback = null;

				// Allocate the initial list of parameters
				if (mParameters == null || mParameters.Length != mParameterInfos.Length)
				{
					mParameters = new Parameter[mParameterInfos.Length];
					for (int i = 0, imax = mParameters.Length; i < imax; ++i)
						mParameters[i] = new Parameter();
				}

				// Save the parameter type
				for (int i = 0, imax = mParameters.Length; i < imax; ++i)
                {
					mParameters[i].expectedType = mParameterInfos[i].ParameterType;
                    mParameters[i].name = mParameterInfos[i].Name;
                }
			}
		}
#endif // REFLECTION_SUPPORT
	}

	/// <summary>
	/// Execute the delegate, if possible.
	/// This will only be used when the application is playing in order to prevent unintentional state changes.
	/// </summary>

	public bool Execute ()
	{
#if !REFLECTION_SUPPORT
		if (isValid)
		{
            if (mRawDelegate) mCachedCallback.DynamicInvoke(null);
			else mTarget.SendMessage(mMethodName, SendMessageOptions.DontRequireReceiver);
			return true;
		}
#else

        if (!mCached || !(mCachedFieldInfo != null || mCachedPropertyInfo != null || mMethod != null))
        {
            Cache();
        }
		
		//check if field or property
		if(mCachedFieldInfo != null)
		{
			if (mParameters != null && mParameters[0] != null)
			{
				//removing cached value, fetching new value when calling the 'get'
				mParameters[0].value = null;

				//apply value in parameter to target property
				mCachedFieldInfo.SetValue(mTarget, mParameters[0].value);
				return true;
			}
		}
		else if(mCachedPropertyInfo != null)
		{
			if (mParameters != null && mParameters[0] != null)
			{
				//removing cached value, fetching new value when calling the 'get'
				mParameters[0].value = null;

				//apply value in parameter to target property
				mCachedPropertyInfo.SetValue(mTarget, mParameters[0].value, null);
				return true;
			}
		}

		if (mCachedCallback != null)
		{
#if !UNITY_EDITOR
            mCachedCallback.DynamicInvoke(null);
#else
			if (Application.isPlaying)
			{
				mCachedCallback.DynamicInvoke(null);
			}
			else if (mCachedCallback.Target != null)
			{
				// There must be an [ExecuteInEditMode] flag on the script for us to call the function at edit time
				System.Type type = mCachedCallback.Target.GetType();
#if UNITY_5_3_OR_NEWER || UNITY_5 || UNITY_4_6 || UNITY_4_5 || UNITY_4_3
                object[] objs = type.GetCustomAttributes(typeof(ExecuteInEditMode), true);
#else
                object[] objs = type.GetCustomAttributes(typeof(ExecuteInEditModeAttribute), true);
#endif
                if (objs != null && objs.Length > 0)
                    mCachedCallback.DynamicInvoke(null);
			}
#endif
			return true;
		}

		if (mMethod != null)
		{
#if UNITY_EDITOR
			// There must be an [ExecuteInEditMode] flag on the script for us to call the function at edit time
			if (mTarget != null && !Application.isPlaying)
			{
				System.Type type = mTarget.GetType();
#if UNITY_5_3_OR_NEWER || UNITY_5 || UNITY_4_6 || UNITY_4_5 || UNITY_4_3
                object[] objs = type.GetCustomAttributes(typeof(ExecuteInEditMode), true);
#else
                object[] objs = type.GetCustomAttributes(typeof(ExecuteInEditModeAttribute), true);
#endif
                if (objs == null || objs.Length == 0)
                    return true;
			}
#endif
			int len = (mParameters != null) ? mParameters.Length : 0;

			if (len == 0)
			{
                try
				{
					mMethod.Invoke(mTarget, null);
				}
				catch (System.ArgumentException ex)
				{
					LogInvokeError(ex);
				}
			}
			else
			{
				// Allocate the parameter array
				if (mArgs == null || mArgs.Length != mParameters.Length)
					mArgs = new object[mParameters.Length];

                Parameter paramItem = null;

				// Set all the parameters
				for (int i = 0, imax = mParameters.Length; i < imax; ++i, paramItem = null)
                {
                    paramItem = mParameters[i];

                    if(paramItem == null)
                        continue;

                    //update the parameter if is a reference (assuming value parameters will never change in realtime)
                    //or if in editor mode (for testing purpose)
                    if (paramItem.paramRefType == ParameterType.Reference || Application.isEditor)
                    {
                        paramItem.value = null;
                    }

                    //obtaining param value
                    mArgs[i] = paramItem.value;
                }

				// Invoke the callback
				try
				{
					mMethod.Invoke(mTarget, mArgs);
				}
				catch (System.ArgumentException ex)
				{
					LogInvokeError(ex);
				}

				// Clear the parameters so that references are not kept
				for (int i = 0, imax = mArgs.Length; i < imax; ++i)
				{
					if (mParameterInfos[i].IsIn || mParameterInfos[i].IsOut)
					{
						mParameters[i].value = mArgs[i];
					}
					mArgs[i] = null;
				}
			}

			return true;
		}
#endif
		return false;
	}

    void LogInvokeError(System.ArgumentException ex)
    {
        string msg = "Error calling ";

		if (mTarget == null) msg += mMethod.Name;
		else msg += mTarget.GetType() + "." + mMethod.Name;
					
		msg += ": " + ex.Message;
		msg += "\n  Expected: ";

		if (mParameterInfos.Length == 0)
		{
			msg += "no arguments";
		}
		else
		{
			msg += mParameterInfos[0];
			for (int i = 1; i < mParameterInfos.Length; ++i)
				msg += ", " + mParameterInfos[i].ParameterType;
		}

		msg += "\n  Received: ";

		if (mParameters.Length == 0)
		{
			msg += "no arguments";
		}
		else
		{
			msg += mParameters[0].type;
			for (int i = 1; i < mParameters.Length; ++i)
				msg += ", " + mParameters[i].type;
		}
		msg += "\n";
		Debug.LogError(msg);
    }

	/// <summary>
	/// Clear the event delegate.
	/// </summary>

	public void Clear ()
	{
		mTarget = null;
		mMethodName = null;
		mRawDelegate = false;
		mCachedCallback = null;
		mCachedFieldInfo = null;
		mCachedPropertyInfo = null;
		mParameters = null;
		mCached = false;
#if REFLECTION_SUPPORT
		mMethod = null;
		mParameterInfos = null;
		mArgs = null;
#endif
	}

	/// <summary>
	/// Convert the delegate to its string representation.
	/// </summary>

	public override string ToString ()
	{
		if (mTarget != null)
		{
			string typeName = mTarget.GetType().ToString();
			int period = typeName.LastIndexOf('/');
			if (period > 0)
                typeName = typeName.Substring(period + 1);

			if (!string.IsNullOrEmpty(methodName))
                return typeName + "/" + methodName;
			else
                return typeName + "/[delegate]";
		}
		return mRawDelegate ? "[delegate]" : null;
	}

	/// <summary>
	/// Execute an entire list of delegates.
	/// </summary>

	static public void Execute (List<EventDelegate> list)
	{
		if (list != null)
		{
			int imax = list.Count;
			for (int i = 0; i < imax; )
			{
				EventDelegate del = list[i];

				if (del != null)
				{
#if !UNITY_EDITOR && !UNITY_FLASH
					try
					{
						del.Execute();
					}
					catch (System.Exception ex)
					{
						if (ex.InnerException != null) Debug.LogError(ex.InnerException.Message);
						else Debug.LogError(ex.Message);
					}
#else
					del.Execute();
#endif

					if (i >= list.Count) break;
					if (list[i] != del) continue;

					if (del.oneShot)
					{
						list.RemoveAt(i);
						continue;
					}
				}
				++i;
			}
		}
	}

	/// <summary>
	/// Convenience function to check if the specified list of delegates can be executed.
	/// </summary>

	static public bool IsValid (List<EventDelegate> list)
	{
		if (list != null)
		{
			EventDelegate del = null;
			for (int i = 0, imax = list.Count; i < imax; ++i, del = null)
			{
				del = list[i];
				if (del != null && del.isValid)
					return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Assign a new event delegate.
	/// </summary>

	static public EventDelegate Set (List<EventDelegate> list, Callback callback)
	{
		if (list != null)
		{
			EventDelegate del = new EventDelegate(callback);
			list.Clear();
			list.Add(del);
			return del;
		}
		return null;
	}

	/// <summary>
	/// Assign a new event delegate.
	/// </summary>

	static public void Set (List<EventDelegate> list, EventDelegate del)
	{
		if (list != null)
		{
			list.Clear();
			list.Add(del);
		}
	}

	/// <summary>
	/// Append a new event delegate to the list.
	/// </summary>

	static public EventDelegate Add (List<EventDelegate> list, Delegate callback) { return Add(list, callback, false); }

	/// <summary>
	/// Append a new event delegate to the list.
	/// </summary>

    static public EventDelegate Add (List<EventDelegate> list, Delegate callback, bool oneShot)
	{
		if (list != null)
		{
			EventDelegate del = null;
			for (int i = 0, imax = list.Count; i < imax; ++i, del = null)
			{
				del = list[i];
				if (del != null && del.Equals(callback))
					return del;
			}

			EventDelegate ed = new EventDelegate(callback);
			ed.oneShot = oneShot;
			list.Add(ed);
			return ed;
		}
		Debug.LogWarning("Attempting to add a callback to a list that's null");
		return null;
	}

	/// <summary>
	/// Append a new event delegate to the list.
	/// </summary>

	static public void Add (List<EventDelegate> list, EventDelegate ev) { Add(list, ev, ev.oneShot); }

	/// <summary>
	/// Append a new event delegate to the list.
	/// </summary>

	static public void Add (List<EventDelegate> list, EventDelegate ev, bool oneShot)
	{
		if (ev.mRawDelegate || ev.target == null || string.IsNullOrEmpty(ev.methodName))
		{
			Add(list, ev.mCachedCallback, oneShot);
		}
		else if (list != null)
		{
			EventDelegate del = null;
			for (int i = 0, imax = list.Count; i < imax; ++i, del = null)
			{
				del = list[i];
				if (del != null && del.Equals(ev))
					return;
			}
			
			EventDelegate copy = new EventDelegate(ev.target, ev.methodName);
			copy.oneShot = oneShot;

			if (ev.mParameters != null && ev.mParameters.Length > 0)
			{
				copy.mParameters = new Parameter[ev.mParameters.Length];
				for (int i = 0; i < ev.mParameters.Length; ++i)
					copy.mParameters[i] = ev.mParameters[i];
			}

			list.Add(copy);
		}
		else Debug.LogWarning("Attempting to add a callback to a list that's null");
	}

	/// <summary>
	/// Remove an existing event delegate from the list.
	/// </summary>

	static public bool Remove (List<EventDelegate> list, Callback callback)
	{
		if (list != null)
		{
			EventDelegate del = null;
			for (int i = 0, imax = list.Count; i < imax; ++i, del = null)
			{
				del = list[i];
				
				if (del != null && del.Equals(callback))
				{
					list.RemoveAt(i);
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Remove an existing event delegate from the list.
	/// </summary>

	static public bool Remove (List<EventDelegate> list, EventDelegate ev)
	{
		if (list != null)
		{
			EventDelegate del = null;
			for (int i = 0, imax = list.Count; i < imax; ++i, del = null)
			{
				del = list[i];

				if (del != null && del.Equals(ev))
				{
					list.RemoveAt(i);
					return true;
				}
			}
		}
		return false;
	}
    
    /// <summary>
    /// Convenience function that converts Class + Function combo into Class.Function representation.
    /// </summary>
    
    static public string GetFuncName (object obj, string method)
    {
        if (obj == null)
            return "<null>";
        
        string type = obj.GetType().ToString();
        
        int period = type.LastIndexOf('/');
        if (period > 0)
            type = type.Substring(period + 1);
        
        return string.IsNullOrEmpty(method) ? type : type + "/" + method;
    }
}

[Serializable]
public class Entry : IComparer<Entry>, IComparable<Entry>
{
	public UnityEngine.Object target;
    public string name;
    
    public Entry(){}
    
	public Entry(UnityEngine.Object target, string name)
    {
        this.target = target;
        this.name = name;
    }
    
    #region IComparable implementation
    
    public int CompareTo(Entry other)
    {
        return name.CompareTo(other.name);
    }
    
    #endregion
    
    #region IComparer implementation
    
    public int Compare(Entry x, Entry y)
    {
        return x.name.CompareTo(y.name);
    }
    
    #endregion
    
    public override bool Equals (object obj)
    {
        if (obj == null)
            return false;
        
        if (obj is Entry)
        {
            Entry entry = obj as Entry;
            
            if (entry == null)
                return false;
            
            if (target == entry.target)
            {
                if(name == entry.name)
                    return true;
            }
        }
        
        return false;
    }
    
    static int entryHash = "Entry".GetHashCode();
    
    /// <summary>
    /// Used in equality operators.
    /// </summary>
    
    public override int GetHashCode ()
    {
        return entryHash;
    }
}

public static class AttributeExtension
{
    /// <summary>
    /// Obtains the first or default attribute from a MemberInfo.
    /// </summary>

    static public T GetAttribute<T>(this MemberInfo memberInfo) where T : Attribute
    {
        return memberInfo.GetCustomAttributes(typeof(T), true).FirstOrDefault() as T;
    }
}
}