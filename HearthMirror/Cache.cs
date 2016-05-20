using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HearthMirror
{
	internal class Cache
	{
		private readonly Dictionary<long, LinkedListNode<Page>> _map = new Dictionary<long, LinkedListNode<Page>>();
		private readonly LinkedList<Page> _pages = new LinkedList<Page>();
		private readonly int _size;

		public Cache(int size)
		{
			_size = size;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public byte[] Get(long key)
		{
			LinkedListNode<Page> node;
			if(!_map.TryGetValue(key, out node))
				return null;
			var value = node.Value.Value;
			_pages.Remove(node);
			_pages.AddLast(node);
			return value;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Add(long key, byte[] value)
		{
			if(_map.Count >= _size)
				RemoveFirst();
			var node = new LinkedListNode<Page>(new Page(key, value));
			_pages.AddLast(node);
			_map.Add(key, node);
		}

		private void RemoveFirst()
		{
			var node = _pages.First;
			_pages.RemoveFirst();
			_map.Remove(node.Value.Key);
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Clear()
		{
			_map.Clear();
			_pages.Clear();
		}
	}

	internal class Page
	{
		public Page(long key, byte[] value)
		{
			Key = key;
			Value = value;
		}

		public long Key { get; }
		public byte[] Value { get; }
	}
}