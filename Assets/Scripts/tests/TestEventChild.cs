using UnityEngine;
using System.Collections;

public class TestEventChild : TestEventsParent
{
    int TestingintInternalMethod(string sendMessage)
    {
        Debug.Log(sendMessage);
        return 0;
    }

    public void MyChildEvent(string childMessage)
    {
        Debug.Log(childMessage);
    }
    
    public void MyChildEvent()
    {
        Debug.Log("overload child method executed");
    }
    
    public string MyOptionalParameters(string childMessage = "test")
    {
        Debug.Log(childMessage);
        return childMessage;
    }
    
    static public bool TestingReturn(string sendMessage)
    {
        Debug.Log(sendMessage);
        return true;
    }
    
    public int TestingReturnInt(string sendMessage)
    {
        Debug.Log(sendMessage);
        return 0;
    }
    
    public void TestingMultipleParameters(string message, float number)
    {
        Debug.Log(message + ", number: " + number);
    }
    
    static void MyStaticMethod(string sendMessage)
    {
        Debug.Log(sendMessage);
    }
    
    void TestingVectorParameter(Vector2 vec2, Vector3 vec3, Vector4 vec4)
    {
        Debug.Log("Testing Vector Parameter:");
		Debug.Log("Vec2: " + vec2);
		Debug.Log("Vec3: " + vec3);
		Debug.Log("Vec4: " + vec4);
    }
    
    void TestSerializedClassParam(DelegateHolder holderClass)
    {
		Debug.Log("Testing Serialized Class Param: " + holderClass.name);
    }
    
    public void TestingColorParam(Color color)
    {
        Debug.Log("Testing Color Parameter: " + color.ToString());
    }
    
    public MonoBehaviour TestMono(string message)
    {
        Debug.Log(message);
        return this;
    }
    
    public Object TestUnityObject(string message)
    {
        Debug.Log(message);
        return this;
    }
    
    public object TestSystemObject(string message)
    {
        Debug.Log(message);
        return 5;
    }
    
//    public void TemplateTest<T>(string param, T someObject)
//    {
//        Debug.Log(param + " object: " + someObject.ToString());
//    }
}
