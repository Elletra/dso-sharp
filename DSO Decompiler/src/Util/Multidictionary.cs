using System.Collections.Generic;
using System.Linq;

namespace DSODecompiler.Util
{
	/// <summary>
	/// Utility class for storing multiple values at the same key.
	/// </summary>
	/// <typeparam name="K"></typeparam>
	/// <typeparam name="V"></typeparam>
	public class Multidictionary<K, V>
	{
		private readonly Dictionary<K, HashSet<V>> storage = new();

		/// <summary>
		/// Adds the specified element to the multidictionary.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns>true if the element is added, false if the element was already present.</returns>
		public bool Add (K key, V value)
		{
			if (!storage.ContainsKey(key))
			{
				storage[key] = new();
			}

			return storage[key].Add(value);
		}

		/// <summary>
		/// Removes the specified element from the multidictionary.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns>true if the element was found and removed, false if the element was not found.</returns>
		public bool Remove (K key, V value)
		{
			if (!storage.ContainsKey(key))
			{
				return false;
			}

			var removed = storage[key].Remove(value);

			/* Remove empty hashsets to save memory. */
			if (storage[key].Count <= 0)
			{
				storage.Remove(key);
			}

			return removed;
		}

		/// <summary>
		/// Clears all values stored in the multidictionary.
		/// </summary>
		public void Clear ()
		{
			/* I could probably get away with just calling storage.Clear() and letting garbage
			   collection take care of things, but I just like being thorough... */
			foreach (var (key, set) in storage)
			{
				set.Clear();
				storage.Remove(key);
			}
		}

		/// <summary>
		/// Clears everything at the specified key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns>true if the key was found and its values were successfully cleared, false if the key was not found.</returns>
		public bool Clear (K key)
		{
			if (!storage.ContainsKey(key))
			{
				return false;
			}

			storage[key].Clear();

			return true;
		}

		public bool ContainsKey (K key) => storage.ContainsKey(key);
		public bool ContainsValue (K key, V value) => storage.ContainsKey(key) && storage[key].Contains(value);

		public IEnumerator<(K, V)> GetEnumerator ()
		{
			foreach (var (key, set) in storage)
			{
				foreach (var value in set)
				{
					yield return (key, value);
				}
			}
		}

		public IEnumerable<K> GetKeys () => storage.Keys;

		public IEnumerable<V> GetValues (K key)
		{
			if (!storage.ContainsKey(key))
			{
				return Enumerable.Empty<V>();
			}

			return storage[key];
		}
	}
}
