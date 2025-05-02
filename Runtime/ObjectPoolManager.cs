using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages object pools for Unity objects. Singleton pattern for global access.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Creates a new object pool for the specified prefab.
    /// </summary>
    /// <param name="prefab">The object to pool</param>
    /// <param name="initialSize">Initial number of objects to create</param>
    /// <param name="maxSize">Maximum number of objects allowed</param>
    /// <returns>Created object pool</returns>
    public ObjectPool CreatePool(Object prefab, int initialSize = 5, int maxSize = 50)
    {
        var poolContainer = new GameObject($"{prefab.name}Pool");
        poolContainer.transform.SetParent(transform);
        var pool = new ObjectPool(prefab, initialSize, maxSize, poolContainer.transform);
        return pool;
    }
}

/// <summary>
/// Non-generic object pool for Unity objects.
/// </summary>
public class ObjectPool
{
    private List<PooledObject> pool;
    private Object prefab;
    private int maxSize;
    private Transform container;

    /// <summary>
    /// Initializes the pool with specified parameters.
    /// </summary>
    public ObjectPool(Object prefab, int initialSize, int maxSize, Transform container)
    {
        this.prefab = prefab;
        this.maxSize = maxSize;
        this.container = container;
        pool = new List<PooledObject>(initialSize);

        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }

    private PooledObject CreateNewObject()
    {
        Object instance = null;

        // Handle different types of Unity objects
        if (prefab is GameObject)
        {
            instance = Object.Instantiate(prefab, container);
            (instance as GameObject)?.SetActive(false);
        }
        else if (prefab is Component)
        {
            // For components, instantiate the GameObject they're attached to
            var component = prefab as Component;
            var go = Object.Instantiate(component.gameObject, container);
            instance = go.GetComponent(component.GetType());
            go.SetActive(false);
        }
        else
        {
            // For other Unity Objects (like ScriptableObjects)
            instance = Object.Instantiate(prefab);
        }

        var pooledObj = new PooledObject(instance);
        pool.Add(pooledObj);
        return pooledObj;
    }

    /// <summary>
    /// Gets an available object from the pool.
    /// </summary>
    /// <returns>Available object or null if pool is full</returns>
    public Object Get()
    {
        var available = pool.Find(p => !p.IsInUse);

        if (available == null && pool.Count < maxSize)
        {
            available = CreateNewObject();
        }

        if (available != null)
        {
            available.IsInUse = true;
            if (available.Object is GameObject go)
            {
                go.SetActive(true);
            }
            else if (available.Object is Component comp)
            {
                comp.gameObject.SetActive(true);
            }
            return available.Object;
        }

        return null;
    }

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <param name="obj">Object to return</param>
    public void Return(Object obj)
    {
        var pooledObj = pool.Find(p => p.Object == obj);
        if (pooledObj != null)
        {
            pooledObj.IsInUse = false;
            if (obj is GameObject go)
            {
                go.SetActive(false);
                go.transform.SetParent(container);
            }
            else if (obj is Component comp)
            {
                comp.gameObject.SetActive(false);
                comp.gameObject.transform.SetParent(container);
            }
        }
    }

    /// <summary>
    /// Returns all objects to the pool.
    /// </summary>
    public void ReturnAll()
    {
        foreach (var item in pool)
        {
            item.IsInUse = false;
            if (item.Object is GameObject go)
            {
                go.SetActive(false);
                go.transform.SetParent(container);
            }
            else if (item.Object is Component comp)
            {
                comp.gameObject.SetActive(false);
                comp.gameObject.transform.SetParent(container);
            }
        }
    }

    /// <summary>
    /// Destroys the pool and all its objects.
    /// </summary>
    public void Destroy()
    {
        foreach (var item in pool)
        {
            if (item.Object is GameObject go)
            {
                Object.Destroy(go);
            }
            else if (item.Object is Component comp)
            {
                Object.Destroy(comp.gameObject);
            }
            else
            {
                Object.Destroy(item.Object);
            }
        }
        pool.Clear();
        if (container != null)
        {
            Object.Destroy(container.gameObject);
        }
    }
}

/// <summary>
/// Wrapper class for pooled objects.
/// </summary>
internal class PooledObject
{
    public Object Object { get; private set; }
    public bool IsInUse { get; set; }

    public PooledObject(Object obj)
    {
        Object = obj;
        IsInUse = false;
    }
}