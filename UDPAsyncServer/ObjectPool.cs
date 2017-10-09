using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;

namespace UDPAsyncServer
{
	public class ObjectPool<T>
	{
        protected ConcurrentBag<T> mBag;
		protected Func<T> mCreateFunc;
		protected Action<T> mResetFunc;

		public int Capacity { get; private set; }
		public int Count { get { return mBag.Count; } }

		public ObjectPool(Func<T> createFunc, Action<T> resetFunc,int maxCapacity)
		{
			Contract.Assume(createFunc != null);
			Contract.Assume(maxCapacity > 0);
            mBag = new ConcurrentBag<T>();
			mCreateFunc = createFunc;
			mResetFunc = resetFunc;
			Capacity = maxCapacity;
		}

		public ObjectPool(int capacity)
        {
			mBag = new ConcurrentBag<T>();
			Capacity = capacity;
		}

		/// <summary>
		/// Get Object
		/// </summary>
		public T GetObject()
		{
			T item;
			if (mBag.TryTake(out item)) return item;
			return mCreateFunc();
		}

		/// <summary>
		/// Release Object
		/// </summary>
		public void PutObject(T obj)
		{
			Contract.Assume(obj != null);

			if (Count >= Capacity)
				return;

			if (mResetFunc != null)
				mResetFunc(obj);

            mBag.Add(obj);
		}

	}


}