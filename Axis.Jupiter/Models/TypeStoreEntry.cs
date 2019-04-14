using Axis.Jupiter.Contracts;
using Axis.Luna.Extensions;
using System;

namespace Axis.Jupiter.Models
{
    public class TypeStoreEntry
    {
        public string TypeName { get; }
        public Type QueryServiceType { get; }
        public Type CommandServiceType { get; }
        public ITypeTransform TypeTransform { get; }

        public TypeStoreEntry(
            string typeName,
            Type queryServiceType,
            Type commandServiceType,
            ITypeTransform typeTransform)
        {
            TypeName = typeName;
            QueryServiceType = queryServiceType;
            CommandServiceType = commandServiceType;
            TypeTransform = typeTransform;

            Validate();
        }

        private void Validate()
        {
            TypeName.ThrowIf(
                string.IsNullOrWhiteSpace,
                new ArgumentException("Invalid TypeName"));

            #region Store Query

            if (QueryServiceType == null)
                throw new Exception("Invalid Store Query Type");

            if (!QueryServiceType.Implements(typeof(IStoreQuery)))
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
