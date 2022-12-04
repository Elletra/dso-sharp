﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DSODecompiler.Util
{
	public class Multidictionary<K, V>
	{
		private Dictionary<K, HashSet<V>> storage = new Dictionary<K, HashSet<V>> ();

		public bool Add (K key, V value)
		{
			if (!ContainsKey (key))
			{
				storage.Add (key, new HashSet<V> ());
			}

			return storage[key].Add (value);
		}

		public bool Remove (K key, V value)
		{
			if (!ContainsKey (key))
			{
				return false;
			}

			return storage[key].Remove (value);
		}

		public bool ContainsKey (K key) => storage.ContainsKey (key);
		public bool ContainsValue (K key, V value) => ContainsKey (key) && storage[key].Contains (value);
		public void Clear () => storage.Clear ();

		public IEnumerator<KeyValuePair<K, V>> GetEnumerator ()
		{
			foreach (var pair in storage)
			{
				var set = pair.Value;

				foreach (var value in set)
				{
					yield return new KeyValuePair<K, V> (pair.Key, value);
				}
			}
		}
	}
}