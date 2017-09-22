using System.Collections.Concurrent;
using System;

public class CObjectPool<T>
{
	private ConcurrentBag<T> _objects;
	private Func<T> _objectGenerator;

	public CObjectPool(Func<T> objectGenerator)
	{
		if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");
		_objects = new ConcurrentBag<T>();
		_objectGenerator = objectGenerator;
	}

	public T GetObject()
	{
		T item;
		if (_objects.TryTake(out item)) return item;
		return _objectGenerator();
	}

	public void PutObject(ref T item)
	{
		_objects.Add(item);
	}
}
