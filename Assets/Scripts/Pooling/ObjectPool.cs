using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}

public class ObjectPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Transform _parentContainer;
    private readonly Stack<T> _inactiveInstances;

    public ObjectPool(T prefab, int initialCapacity, Transform parentContainer = null)
    {
        _prefab = prefab;
        _parentContainer = parentContainer;
        _inactiveInstances = new Stack<T>(initialCapacity);
    }

    public T Get()
    {
        T instance = _inactiveInstances.Count > 0 
            ? _inactiveInstances.Pop() 
            : Object.Instantiate(_prefab, _parentContainer);

        instance.gameObject.SetActive(true);
        
        if (instance is IPoolable poolable)
        {
            poolable.OnSpawn();
        }

        return instance;
    }

    public void Return(T instance)
    {
        if (instance == null) return;

        if (instance is IPoolable poolable)
        {
            poolable.OnDespawn();
        }

        instance.gameObject.SetActive(false);
        _inactiveInstances.Push(instance);
    }

    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            T instance = Object.Instantiate(_prefab, _parentContainer);
            instance.gameObject.SetActive(false);
            _inactiveInstances.Push(instance);
        }
    }
}