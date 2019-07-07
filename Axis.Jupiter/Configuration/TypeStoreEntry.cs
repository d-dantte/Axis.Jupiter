using Axis.Jupiter.Contracts;
using Axis.Luna.Extensions;
using System;

namespace Axis.Jupiter.Configuration
{
    public abstract class TypeStoreEntry
    {
        public string TypeName { get; }
        public Type QueryServiceType { get; }
        public Type CommandServiceType { get; }
        public ITypeMapper TypeMapper { get; }

        public TypeStoreEntry(
            string typeName,
            Type queryServiceType,
            Type commandServiceType,
            ITypeMapper mapper)
        {
            TypeName = typeName;
            QueryServiceType = queryServiceType;
            CommandServiceType = commandServiceType;
            TypeMapper = mapper;

            Validate();
        }

        public TypeStoreEntry(
            string typeName,
            Type commandServiceType,
            ITypeMapper mapper)

        :this(
            typeName,
            null,
            commandServiceType,
            mapper)
        {
        }

        private void Validate()
        {
            TypeName.ThrowIf(
                string.IsNullOrWhiteSpace,
                new ArgumentException("Invalid TypeName"));

            #region Store Query

            if (QueryServiceType?.Implements(typeof(IStoreQuery)) == false)
                throw new Exception($"Invalid Store Query Type: does not implement/extend {typeof(IStoreQuery).FullName}");

            #endregion

            #region Store Command

            if (CommandServiceType == null)
                throw new Exception("Invalid Store Command Type");

            if (!CommandServiceType.Implements(typeof(IStoreCommand)))
                throw new Exception($"Invalid Store Command Type: does not implement/extend {typeof(IStoreCommand).FullName}");

            #endregion
        }
    }
}
