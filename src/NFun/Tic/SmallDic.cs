using System;
using System.Collections.Generic;
using System.Linq;

namespace NFun.Tic
{ 
    public class SmallStringDictionary<T>
    {
        enum DicState
        {
            FullArray = 0,
            CanAddToArray = 1,
            Dictionary = 2,
        }

        private DicState _state = DicState.CanAddToArray;
        private const int MaxArrayLength = 4;
        private Dictionary<string, T> _dictionary = null;
        private string[] _keys = new string[MaxArrayLength];
        private T[] _values = new T[MaxArrayLength];
        private int _currentArraySize = 0;

        public SmallStringDictionary(int capacity)
        {
            if (capacity > MaxArrayLength)
            {
                _dictionary = new Dictionary<string, T>();
                _state = DicState.Dictionary;
            }
        }

        public SmallStringDictionary()
        {
            
        }
        public void Clear()
        {
            _dictionary = null;
            _currentArraySize = 0;
        }

        public int Count  => _dictionary?.Count ?? _currentArraySize;
        public bool IsReadOnly =>  false;

      
        public IEnumerable<T> ValuesWithDefaults
        {
            get
            {
                if (_dictionary == null)
                    return _values;
                return _dictionary.Values;
            }
        }

        public IEnumerable<T> Values => _state switch 
            {
                DicState.CanAddToArray => _values.Take(_currentArraySize),
                DicState.FullArray => _values,
                _ => _dictionary!.Values
            };

        public void Add(string key, T value)
        {
            if (_state== DicState.CanAddToArray)
            {
                _keys  [_currentArraySize] = key;
                _values[_currentArraySize] = value;
                _currentArraySize++;
                if (_currentArraySize >= MaxArrayLength) 
                    TransformToDictionary();
                return;
            }

            if (_state == DicState.FullArray) 
                TransformToDictionary();
            
            _dictionary.Add(key,value);
        }

        private void TransformToDictionary()
        {
            _dictionary = new Dictionary<string, T>();
            for (int i = 0; i < _currentArraySize; i++)
            {
                _dictionary.Add(_keys[i], _values[i]);
            }
            _state = DicState.Dictionary;
        }
        public bool ContainsKey(string key)
        {
            if (_dictionary == null)
            {
                for (int i = 0; i < _currentArraySize; i++)
                {
                    if (_keys[i] == key)
                        return true;
                }

                return false;
            }
            return _dictionary.ContainsKey(key);
        }

        public bool TryGetValue(string key, out T value)
        {
            if (_dictionary == null)
            {
                for (int i = 0; i < _currentArraySize; i++)
                {
                    if (_keys[i] == key)
                    {
                        value = _values[i];
                        return true;
                    }
                }

                value = default;
                return false;
            }
            return _dictionary.TryGetValue(key, out value);
        }

        public T this[string key]
        {
            get
            {
                if (_dictionary == null)
                {
                    for (int i = 0; i < _currentArraySize; i++)
                        if (_keys[i] == key)
                            return _values[i];
                    throw new KeyNotFoundException(key);
                }
                return _dictionary[key];
            }
        }
    }
}