using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace Axis.Jupiter.Europa.Utils
{
    public static class MetadataExtensions
    {
        public static XDocument GetEdmx(this DbModel dbm)
        {
            XDocument doc;
            using (var memoryStream = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(memoryStream, new XmlWriterSettings { Indent = true }))
                {
                    EdmxWriter.WriteEdmx(dbm, xmlWriter);
                }

                memoryStream.Position = 0;

                doc = XDocument.Load(memoryStream);
            }
            return doc;
        }
    }

    public class EFMappings
    {
        public EFMappings(DbModel model)
        {
            model.ConceptualToStoreMapping
                 .EntitySetMappings
                 .SelectMany(_esm => _esm.EntityTypeMappings.SelectMany(_etm => _etm.Fragments.Select(_ef =>
                 {
                     var clrType = _etm.EntityTypes.FirstOrDefault()
                         .MetadataProperties
                         .FirstOrDefault(_mdp => _mdp.Name == "http://schemas.microsoft.com/ado/2013/11/edm/customannotation:ClrType")
                         .Value
                         .As<Type>();
                 
                     return new TypeModel
                     {
                         ClrType = clrType,
                         MappedTable =  _ef.StoreEntitySet.Table,
                         Properties = _ef.PropertyMappings.Where(_pm => _pm.Is<ScalarPropertyMapping>()).Select(_pm =>
                         {
                             var column = _pm.As<ScalarPropertyMapping>().Column;
                             return new PropertyModel
                             {
                                 ClrProperty = clrType.GetProperty(_pm.Property.Name),
                                 MappedProperty = column.Name,
                                 Key = column.IsStoreGeneratedIdentity ? PropertyModel.KeyMode.StoreGenerated :
                                       column.DeclaringType.As<EntityType>().KeyProperties.Contains(column) ? PropertyModel.KeyMode.SourceGenerated :
                                       PropertyModel.KeyMode.None
                             };
                         })
                     };
                 })))
                 .ForAll((_cnt, _next) => _models.Add(_next));
        }

        private List<TypeModel> _models = new List<TypeModel>();

        public IEnumerable<TypeModel> ModelMappings => _models.ToArray();

        public TypeModel MappingFor<Entity>() => MappingFor(typeof(Entity));
        public TypeModel MappingFor(Type entityType) => _models.FirstOrDefault(_model => _model.ClrType == entityType);
    }

    public class TypeModel
    {
        public Type ClrType { get; internal set; }
        public string MappedTable { get; internal set; }

        private HashSet<PropertyModel> _props = new HashSet<PropertyModel>();
        public IEnumerable<PropertyModel> Properties
        {
            get { return _props.ToArray(); }
            internal set
            {
                _props.Clear();
                if (value != null) _props.AddRange(value);
            }
        }
    }

    public class PropertyModel
    {
        public enum KeyMode { None, StoreGenerated, SourceGenerated }

        public PropertyInfo ClrProperty { get; internal set; }
        public string MappedProperty { get; internal set; }
        public bool IsKey => Key != KeyMode.None;

        public KeyMode Key { get; internal set; }


        public override bool Equals(object obj)
            => obj.As<PropertyModel>().Pipe(_pm => _pm?.ClrProperty == ClrProperty && 
                                                   _pm?.MappedProperty == MappedProperty);
        public override int GetHashCode() => this.PropertyHash();
    }
}
