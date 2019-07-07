using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Jupiter.Configuration
{
    public class TypeStoreMap
    {
        private readonly Dictionary<string, TypeStoreEntry> _entries = new Dictionary<string, TypeStoreEntry>();

        public TypeStoreMap(params TypeStoreEntry[] entries)
        {
            entries
                .ThrowIf(HasNull, new ArgumentException("Invalid type entry found"))
                .ForAll(AddEntry);
        }

        public TypeStoreEntry Entry(string typeName)
        => _entries.TryGetValue(typeName, out var TypeStoreEntry) 
            ? TypeStoreEntry 
            : null;

        internal TypeStoreEntry[] Entries() => _entries.Values.ToArray();


        private void AddTypeStoreEntry(TypeStoreEntry entry)
        {
            if (entry == null)
                throw new ArgumentException("Invalid TypeStoreEntry specified: null");

            if (_entries.ContainsKey(entry.TypeName))
                throw new Exception("Duplicate Store Name detected");
            
            _entries.Add(entry.TypeName, entry);
        }

        private void AddEntry(TypeStoreEntry entry) => _entries.Add(entry.TypeName, entry);


        private static bool HasNull(TypeStoreEntry[] tseArr) => tseArr.Any(t => t == null);
    }
}
