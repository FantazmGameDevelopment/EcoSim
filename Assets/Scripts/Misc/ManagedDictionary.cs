using System.Collections.Generic;

namespace System.Collections.Generic
{
	public class ManagedDictionary<TKey, TValue> : Dictionary<TKey, TValue>
	{
		public delegate void KeyValueDelegate (TKey key, TValue value);

		private Dictionary<TKey, List<KeyValueDelegate>> updateCallbacks;
		private Dictionary<TKey, TValue> dict;

		public ManagedDictionary () 
		{
			dict = new Dictionary<TKey, TValue> ();
			updateCallbacks = new Dictionary<TKey, List<KeyValueDelegate>> ();
		}

		public bool ContainsKey(TKey key)
		{
			return dict.ContainsKey (key);
		}

		public void Add (TKey key, TValue value)
		{
			dict.Add (key, value);
			CheckLinks (key, value);
		}

		public bool Remove(TKey key)
		{
			return dict.Remove (key);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			return dict.TryGetValue (key, out value);
		}

		public void Add (KeyValuePair<TKey, TValue> item)
		{
			dict.Add (item.Key, item.Value);
			CheckLinks (item.Key, item.Value);
		}

		public void Clear()
		{
			dict.Clear ();
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return dict.GetEnumerator ();
		}

		public TValue this[TKey key]
		{
			get
			{
				/*TValue val;
				if (TryGetValue (key, out val)) {
					return val;
				}
				UnityEngine.Debug.LogError (string.Format ("Key could not be found '{0}'", key.ToString ()));
				return default (TValue);*/
				return dict [key];
			}
			set
			{
				dict [key] = value;
				CheckLinks (key, value);
			}
		}

		public ICollection<TKey> Keys
		{
			get {
				return dict.Keys;
			}
		}

		public ICollection<TValue> Values
		{
			get {
				return dict.Values;
			}
		}

		public int Count
		{
			get {
				return dict.Count;
			}
		}

		public bool Contains (KeyValuePair<TKey, TValue> item)
		{
			//throw new System.NotImplementedException ();
			return dict.ContainsKey (item.Key);
		}

		public void CopyTo (KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			//throw new System.NotImplementedException ();
		}

		public bool Remove (KeyValuePair<TKey, TValue> item)
		{
			//throw new System.NotImplementedException ();
			return dict.Remove (item.Key);
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public bool AddUpdateCallback (TKey key, KeyValueDelegate callback) 
		{
			// Check for valid key and callback
			if (key == null || callback == null) return false;
			
			// Check if the variable exists in the links
			if (updateCallbacks.ContainsKey (key))
				updateCallbacks [key].Add (callback);
			else 
				updateCallbacks.Add (key, new List<KeyValueDelegate> () { callback });
			
			return true;
		}
		
		private void CheckLinks (TKey key, TValue value)
		{
			// TODO: Check if the value is updated/new
			
			// Check if we have update callbacks
			List<KeyValueDelegate> callbacks;
			if (updateCallbacks.TryGetValue (key, out callbacks)) {
				foreach (KeyValueDelegate cb in callbacks) {
					if (cb != null)
						cb (key, value);
				}
			}
		}
	}
}
