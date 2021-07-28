using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnityEngineExtensions
{
    /// <summary>
    /// Returns the component of Type type. If one doesn't already exist on the GameObject it will be added.
    /// </summary>
    /// <typeparam name="T">The type of Component to return.</typeparam>
    /// <param name="gameObject">The GameObject this Component is attached to.</param>
    /// <returns>Component</returns>
    static public T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
    }
    static public void DestroyIfExists<T>(this GameObject gameObject) where T : Component
    {
        T t = gameObject.GetComponent<T>();
        if(t)
        {
            UnityEngine.GameObject.DestroyImmediate(t);
        }
    }
    public static void Clear(this Transform transform)
    {
        foreach(Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    public static Vector2 GetXZVector2(this Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.z);
    }
    
    public static Vector3 ToXZVector3(this Vector2 vector2)
    {
        return new Vector3(vector2.x, 0, vector2.y);
    }
    public static Vector3 ToXZVector3(this Vector2Int vector2, float yValue = 0f)
    {
        return new Vector3(vector2.x, yValue, vector2.y);
    }
}
