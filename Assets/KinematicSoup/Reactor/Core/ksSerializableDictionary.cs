using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KS.Reactor.Client.Unity
{
    /// <summary>
    /// Extends the <see cref="IDictionary"/> interface with functions used by the dictionary property drawer to detect
    /// duplicate keys. Because the contents of the dictionary are serialized in a list, it is possible for the list to
    /// contain duplicate keys.
    /// </summary>
    public interface ksISerializableDictionary : IDictionary, ISerializationCallbackReceiver
    {
        /// <summary>Checks if the key at the <paramref name="index"/> is a duplicate.</summary>
        /// <param name="index">Index to check.</param>
        /// <returns>True if the key at the <paramref name="index"/> is a duplicate.</returns>
        bool IsDuplicateKeyAt(int index);

        /// <summary>Checks if there are any duplicate keys.</summary>
        /// <returns>True if there are any duplicate keys.</returns>
        bool HasDuplicateKeys();
    }

    /// <summary>
    /// A dictionary that implements <see cref="ISerializationCallbackReceiver"/> for serializing key value data.
    /// </summary>
    [Serializable]
    public class ksSerializableDictionary<Key, Value> : IDictionary<Key, Value>, ksISerializableDictionary
    {
        /// <summary>
        /// Key/value struct. We define our own struct instead of using KeyValuePair because Unity cannot serialize 
        /// KeyValuePairs.
        /// </summary>
        [Serializable]
        private struct KeyValue
        {
            /// <summary>Key</summary>
            public Key Key;
            /// <summary>Value</summary>
            public Value Value;

            /// <summary>Constructor</summary>
            /// <param name="key">Key</param>
            /// <param name="value">Value</param>
            public KeyValue(Key key, Value value)
            {
                Key = key;
                Value = value;
            }
        };

        // Serialized list of keys and values.
        [SerializeField] 
        private List<KeyValue> m_list = new List<KeyValue>();

        private Dictionary<Key, Value> m_dict = new Dictionary<Key, Value>();
        private HashSet<Key> m_duplicateKeys = null;
        private bool m_dirty = false;

        /// <summary>Key collection.</summary>
        public ICollection<Key> Keys => m_dict.Keys;

        /// <summary>Value collection.</summary>
        public ICollection<Value> Values => m_dict.Values;

        /// <summary>The number of key/value pairs in the dictionary.</summary>
        public int Count => m_dict.Count;

        /// <summary>Is the dictionary read-only?</summary>
        public bool IsReadOnly => ((ICollection<KeyValuePair<Key, Value>>)m_dict).IsReadOnly;

        /// <summary>Is the dictionary a fixed-size?</summary>
        bool IDictionary.IsFixedSize => ((IDictionary)m_dict).IsFixedSize;

        /// <summary>Key collection.</summary>
        ICollection IDictionary.Keys => ((IDictionary)m_dict).Keys;

        /// <summary>Value collection.</summary>
        ICollection IDictionary.Values => ((IDictionary)m_dict).Values;

        /// <summary>Is the collection thread-safe? See <see cref="ICollection.IsSynchronized"/></summary>
        bool ICollection.IsSynchronized => ((ICollection)m_dict).IsSynchronized;

        /// <summary>
        /// An object that can be used to synchronize access to the collection. See <see cref="ICollection.SyncRoot"/>.
        /// </summary>
        object ICollection.SyncRoot => ((ICollection)m_dict).SyncRoot;

        /// <summary>Gets or sets the value associated with a key.</summary>
        /// <param name="key">Key</param>
        /// <returns>The value for the key.</returns>
        object IDictionary.this[object key] { get => this[(Key)key]; set => this[(Key)key] = (Value)value; }

        /// <summary>Gets or sets the value associated with a key.</summary>
        /// <param name="key">Key</param>
        /// <returns>The value for the key.</returns>
        public Value this[Key key]
        {
            get { return m_dict[key]; }
            set
            {
                m_dirty = true;
                m_dict[key] = value;
            }
        }

        /// <summary>Constructor</summary>
        public ksSerializableDictionary()
        {
            
        }

        /// <summary>Constructor</summary>
        /// <param name="keyValues">Key value pairs to populate the dictionary with.</param>
        public ksSerializableDictionary(ICollection<KeyValuePair<Key, Value>> keyValues)
        {
            foreach (KeyValuePair<Key, Value> pair in keyValues)
            {
                this[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// Copies all dictionary values into the serialized key/value list if the dictionary is dirty.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (m_dirty)
            {
                m_list.Clear();
                foreach (var pair in this)
                {
                    m_list.Add(new KeyValue(pair.Key, pair.Value));
                }
                m_dirty = false;
            }
        }

        /// <summary>Copies key and value data from the serialized list into the dictionary.</summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            m_dict.Clear();
            if (m_duplicateKeys != null)
            {
                m_duplicateKeys.Clear();
            }
            for (var i = 0; i < m_list.Count; i++)
            {
                KeyValue pair = m_list[i];
                if (pair.Key == null)
                {
                    // Assets references become null for deleted assets. Remove null keys from the list.
                    m_list.RemoveAt(i);
                    i--;
                    continue;
                }
                if (!m_dict.ContainsKey(pair.Key))
                {
                    m_dict.Add(pair.Key, pair.Value);
                }
#if UNITY_EDITOR
                // If we're in the editor, track duplicate keys for the inspector GUI.
                else
                {
                    if (m_duplicateKeys == null)
                    {
                        m_duplicateKeys = new HashSet<Key>();
                    }
                    m_duplicateKeys.Add(pair.Key);
                }
#endif
            }
#if !UNITY_EDITOR
            // If we're not in the editor, we don't need this data anymore. 
            m_list.Clear();
#endif
            m_dirty = false;
        }

        /// <summary>Checks if the key at the <paramref name="index"/> is a duplicate.</summary>
        /// <param name="index">Index to check.</param>
        /// <returns>True if the key at the <paramref name="index"/> is a duplicate.</returns>
        bool ksISerializableDictionary.IsDuplicateKeyAt(int index)
        {
            return m_duplicateKeys != null && index >= 0 && index < m_list.Count && 
                m_duplicateKeys.Contains(m_list[index].Key);
        }

        /// <summary>Checks if there are any duplicate keys.</summary>
        /// <returns>True if there are any duplicate keys.</returns>
        bool ksISerializableDictionary.HasDuplicateKeys()
        {
            return m_duplicateKeys != null && m_duplicateKeys.Count > 0;
        }

        /// <summary>
        /// Adds a key/value pair to the dictionary. Throws an exception if the key is already in the dictionary.
        /// See <see cref="Dictionary{TKey, TValue}.Add(TKey, TValue)"/>.
        /// </summary>
        /// <param name="key">Key to add.</param>
        /// <param name="value">Value to add.</param>
        public void Add(Key key, Value value)
        {
            m_dict.Add(key, value);
            m_dirty = true;
        }

        /// <summary>
        /// Checks if a key is in the dictionary. See <see cref="Dictionary{TKey, TValue}.ContainsKey(TKey)"/>.
        /// </summary>
        /// <param name="key">Key to check for.</param>
        /// <returns>True if the key is in the dictionary.</returns>
        public bool ContainsKey(Key key)
        {
            return m_dict.ContainsKey(key);
        }

        /// <summary>
        /// Checks if a value is in the dictionary. See <see cref="Dictionary{TKey, TValue}.ContainsValue(TValue)"/>.
        /// </summary>
        /// <param name="value">Value to check for.</param>
        /// <returns>True if the value is in the dictionary.</returns>
        public bool ContainsValue(Value value)
        {
            return m_dict.ContainsValue(value);
        }

        /// <summary>
        /// Removes a key from the dictionary. See <see cref="Dictionary{TKey, TValue}.Remove(TKey)"/>.
        /// </summary>
        /// <param name="key">Key to remove.</param>
        /// <returns>
        /// True if the key was removed from the dictionary. False if the key was not in the dictionary.
        /// </returns>
        public bool Remove(Key key)
        {
            if (m_dict.Remove(key))
            {
                m_dirty = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to get the value for a key from the dictionary. See 
        /// <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)"/>.
        /// </summary>
        /// <param name="key">Key to get value for.</param>
        /// <param name="value">The value for the key.</param>
        /// <returns>True if the key was in the dictionary.</returns>
        public bool TryGetValue(Key key, out Value value)
        {
            return m_dict.TryGetValue(key, out value);
        }

        /// <summary>Adds a key/value pair to the dictionary. See <see cref="ICollection{T}.Add(T)"/>.</summary>
        /// <param name="item">Item to add to the dictionary.</param>
        void ICollection<KeyValuePair<Key, Value>>.Add(KeyValuePair<Key, Value> item)
        {
            ((ICollection<KeyValuePair<Key, Value>>)m_dict).Add(item);
            m_dirty = true;
        }

        /// <summary>Clears the dictionary. See <see cref="Dictionary{TKey, TValue}.Clear"/>.</summary>
        public void Clear()
        {
            m_dict.Clear();
            m_list.Clear();
            m_duplicateKeys = null;
            m_dirty = false;
        }

        /// <summary>
        /// Checks if a key/value pair is in the dictionary. See <see cref="ICollection{T}.Contains(T)"/>.
        /// </summary>
        /// <param name="item">Item to check for.</param>
        /// <returns>True if the key/value pair is in the dictionary.</returns>
        bool ICollection<KeyValuePair<Key, Value>>.Contains(KeyValuePair<Key, Value> item)
        {
            return ((ICollection<KeyValuePair<Key, Value>>)m_dict).Contains(item);
        }

        /// <summary>
        /// Copies the contents of the dictionary to an array. See <see cref="ICollection{T}.CopyTo(T[], int)"/>.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="arrayIndex">The index to start copying to.</param>
        void ICollection<KeyValuePair<Key, Value>>.CopyTo(KeyValuePair<Key, Value>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<Key, Value>>)m_dict).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes a key/value pair from the dictionary. See <see cref="ICollection{T}.Remove(T)"/>.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        /// <returns>True if the key/value pair was removed from the dictionary.</returns>
        bool ICollection<KeyValuePair<Key, Value>>.Remove(KeyValuePair<Key, Value> item)
        {
            if (((ICollection<KeyValuePair<Key, Value>>)m_dict).Remove(item))
            {
                m_dirty = true;
                return true;
            }
            return false;
        }

        /// <summary>Enumerates the key/value pairs in the dictionary.</summary>
        /// <returns>Enumerator</returns>
        public IEnumerator<KeyValuePair<Key, Value>> GetEnumerator()
        {
            return m_dict.GetEnumerator();
        }

        /// <summary>Enumerates the key/value pairs in the dictionary.</summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)m_dict).GetEnumerator();
        }

        /// <summary>
        /// Adds a key/value pair to the dictionary. Throws an exception if the key is already in the dictionary.
        /// See <see cref="IDictionary.Add(object, object)"/>.
        /// </summary>
        /// <param name="key">Key to add.</param>
        /// <param name="value">Value to add.</param>
        void IDictionary.Add(object key, object value)
        {
            Add((Key)key, (Value)value);
        }

        /// <summary>Checks if a key is in the dictionary. See <see cref="IDictionary.Contains(object)"/>.</summary>
        /// <param name="key">Key to check for.</param>
        /// <returns>True if the key is in the dictionary.</returns>
        bool IDictionary.Contains(object key)
        {
            return ((IDictionary)m_dict).Contains(key);
        }

        /// <summary>Enumerates the key/value pairs in the dictionary.</summary>
        /// <returns>Enumerator</returns>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary)m_dict).GetEnumerator();
        }

        /// <summary>Removes a key from the dictionary. See <see cref="IDictionary.Remove(object)"/>.</summary>
        /// <param name="key">Key to remove.</param>
        void IDictionary.Remove(object key)
        {
            int count = Count;
            ((IDictionary)m_dict).Remove(key);
            if (Count != count)
            {
                m_dirty = true;
            }
        }

        /// <summary>
        /// Copies the contents of the dictionary to an array. See <see cref="ICollection.CopyTo(Array, int)"/>.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="index">The index to start copying to.</param>
        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)m_dict).CopyTo(array, index);
        }
    }
}
