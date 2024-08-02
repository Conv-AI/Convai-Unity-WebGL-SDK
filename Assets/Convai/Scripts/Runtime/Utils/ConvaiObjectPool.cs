using System.Collections.Generic;

public class ConvaiObjectPool<T> where T : class, new()
{
    private readonly Stack<T> _objects;

    public ConvaiObjectPool(int initialCapacity)
    {
        _objects = new Stack<T>(initialCapacity);

        for (int i = 0; i < initialCapacity; i++) _objects.Push(new T());
    }

    public T GetObject()
    {
        return _objects.Count > 0 ? _objects.Pop() : new T();
    }

    public void ReleaseObject(T obj)
    {
        _objects.Push(obj);
    }
}