using UnityEngine;

public class FixedPool<T> where T : Component, IPoolable
{
    private readonly T[] _instances;
    private int _head;

    public FixedPool(T prefab, int capacity, Transform parent)
    {
        _instances = new T[capacity];
        _head = 0;

        for (int i = 0; i < capacity; i++)
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(false);
            _instances[i] = instance;
        }
    }

    public T Get()
    {
        T instance = _instances[_head];

        if (instance.gameObject.activeSelf)
        {
            instance.OnDespawn();
            instance.gameObject.SetActive(false);
        }

        _head = (_head + 1) % _instances.Length;

        instance.gameObject.SetActive(true);
        instance.OnSpawn();

        return instance;
    }

    public void Return(T instance)
    {
        instance.OnDespawn();
        instance.gameObject.SetActive(false);
    }
}