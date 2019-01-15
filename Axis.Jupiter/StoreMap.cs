using System;
using System.Collections.Generic;
using Axis.Jupiter.Contracts;
using Axis.Luna.Extensions;

namespace Axis.Jupiter
{
    public class StoreMap
    {
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

        /// <summary>
        /// This property must always have a value
        /// </summary>
        public Entry Default { get; }
        

        public StoreMap(Entry defaultEntry, params Entry[] entries)
        {
            Default = defaultEntry ?? throw new ArgumentException("Invalid Default Entry specified: null");
        }

        public Entry StoreEntry(string storeName) 
        => _entries.TryGetValue(storeName, out var entry)? entry: null;


        private void AddEntry(Entry entry)
        {
            if (entry == null)
                throw new ArgumentException("Invalid Entry specified: null");

            if (_entries.ContainsKey(entry.StoreName))
                throw new Exception("Duplicate Store Name detected");

            entry.Validate();

            _entries.Add(entry.StoreName, entry);
        }


        public sealed class Entry
        { 
            public string StoreName { get; set; }
            public Type StoreQueryType { get; set; }
            public Type StoreCommandType { get; set; }

            public Entry CloneFor(string storeName) => new Entry
            {
                StoreName = storeName,
                StoreQueryType = StoreQueryType,
                StoreCommandType = StoreCommandType
            };

            public void Validate()
            {
                #region Store Query

                if(StoreQueryType == null)
                    throw new Exception("Invalid Store Query Type");

                if(!StoreQueryType.Implements(typeof(IStoreQuery)))
                    throw new Exception($"Invalid Store Query Type: does not implement/extend {typeof(IStoreQuery).FullName}");

                #endregion

                #region Store Command

                if (StoreCommandType == null)
                    throw new Exception("Invalid Store Command Type");

                if (!StoreCommandType.Implements(typeof(IStoreCommand)))
                    throw new Exception($"Invalid Store Command Type: does not implement/extend {typeof(IStoreCommand).FullName}");

                #endregion
            }
        }
    }
}
