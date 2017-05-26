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
                     var clrType = _etm.EntityType
                         .MetadataProperties
                         .FirstOrDefault(_mdp => _mdp.Name == "http://schemas.microsoft.com/ado/2013/11/edm/customannotation:ClrType")
                         .Value
                         .Cast<Type>(); //<-- as Type

                     var typeModel = new TypeModel
                     {
                         MappingRoot = this,
                         ClrType = clrType,
                         BaseType = _etm.EntityType.BaseType?.MetadataProperties
                            .FirstOrDefault(_mdp => _mdp.Name == "http://schemas.microsoft.com/ado/2013/11/edm/customannotation:ClrType")?
                            .Value.Cast<Type>(),
                         MappedTable = _ef.StoreEntitySet.Table,
                         ScalarProperties = _ef.PropertyMappings
                            .Where(_pm => _pm is ScalarPropertyMapping)
                            .Where(_pm => _pm.Property.DeclaringType == _etm.EntityType)
                            .Select(_pm =>
                            {
                                var column = _pm.Cast<ScalarPropertyMapping>().Column;
                                return new ScalarPropertyModel
                                {
                                    ClrProperty = clrType.GetProperty(_pm.Property.Name),
                                    MappedColumn = column.Name,
                                    Key = column.IsStoreGeneratedIdentity ? ScalarPropertyModel.KeyMode.StoreGenerated :
                                          column.DeclaringType.Cast<EntityType>().KeyProperties.Contains(column) ? ScalarPropertyModel.KeyMode.SourceGenerated :
                                          ScalarPropertyModel.KeyMode.None,
                                    _propertyMapping = column
                                };
                            }),
                         ComplexProperties = _ef.PropertyMappings
                            .Where(_pm => _pm is ComplexPropertyMapping)
                            .Cast<ComplexPropertyMapping>()
                            .Select(ExtractComplexTypeModel)
                     };

                     typeModel.NavigationProperties = _etm.EntityType.NavigationProperties
                        .Where(_np => _np.DeclaringType == _etm.EntityType)
                        .Select(_np =>
                        {
                            var association = model.ConceptualModel.AssociationTypes.FirstOrDefault(_ass => _ass.Name == _np.RelationshipType.Name);
                            var isSource = association.Constraint.FromRole.TypeUsage.EdmType.Name.Contains(_etm.EntityType.Name);
                            var constraint = _np.RelationshipType.Cast<AssociationType>().Constraint;
                            return new NavigationPropertyModel
                            {
                                ClrProperty = clrType.GetProperty(_np.Name),
                                LocalKeys = isSource ?
                                            typeModel.ScalarProperties.Where(_sp => constraint.FromProperties.Any(_p => _p.Name == _sp.ClrProperty.Name)):
                                            typeModel.ScalarProperties.Where(_sp => constraint.ToProperties.Any(_p => _p.Name == _sp.ClrProperty.Name)),
                                ExternalKeys = !isSource?
                                               constraint.FromProperties.Select(_p => _p.Name) :
                                               constraint.ToProperties.Select(_p => _p.Name)
                            };
                        });

                     return typeModel;
                 })))
                 .UsingEach(_typeModels.Add)

                 //extract table models
                 .GroupBy(_tm => _tm.MappedTable)
                 .Select(_group => new TableModel
                 {
                     Name = _group.Key,
                     TypeModels = _group
                 })
                 .ForAll(_tableModels.Add); //<-- ToList().ForEach(...)
        }

        private List<TypeModel> _typeModels = new List<TypeModel>();
        private List<ComplexTypeModel> _complexTypeModels = new List<ComplexTypeModel>();
        private List<TableModel> _tableModels = new List<TableModel>();

        public IEnumerable<TypeModel> TypeModelMappings => _typeModels.ToArray();
        public IEnumerable<TableModel> TableModelMappings => _tableModels.ToArray();

        public TypeModel MappingFor<Entity>() => MappingFor(typeof(Entity));
        public TypeModel MappingFor(Type entityType) => this.MappingsFor(entityType).FirstOrDefault();

        public IEnumerable<TypeModel> MappingsFor<Entity>() => MappingsFor(typeof(Entity));
        public IEnumerable<TypeModel> MappingsFor(Type entityType) => _typeModels.Where(_model => _model.ClrType == entityType);

        public TableModel TableMappingFor<Entity>() => TableMappingFor(typeof(Entity));
        public TableModel TableMappingFor(Type entityType) => _tableModels.FirstOrDefault(_tm => _tm.TypeModels.Any(_tym => _tym.ClrType == entityType));

        private ComplexTypeModel ExtractComplexTypeModel(ComplexPropertyMapping mapping)
        {
            var clrProp = mapping.Property.MetadataProperties["ClrPropertyInfo"].Value as PropertyInfo;
            var _et = mapping.TypeMappings.FirstOrDefault();
            return new ComplexTypeModel
            {
                SourceProperty = clrProp,
                ScalarProperties = _et.PropertyMappings
                    .Where(_pm => _pm is ScalarPropertyMapping)
                    .Where(_pm => clrProp.PropertyType.GetProperty(_pm.Property.Name).DeclaringType == clrProp.PropertyType)
                    .Select(_pm =>
                    {
                        var column = _pm.Cast<ScalarPropertyMapping>().Column;
                        return new ScalarPropertyModel
                        {
                            ClrProperty = clrProp.PropertyType.GetProperty(_pm.Property.Name),
                            MappedColumn = column.Name,
                            Key = column.IsStoreGeneratedIdentity ? ScalarPropertyModel.KeyMode.StoreGenerated :
                                  column.DeclaringType.Cast<EntityType>().KeyProperties.Contains(column) ? ScalarPropertyModel.KeyMode.SourceGenerated :
                                  ScalarPropertyModel.KeyMode.None,
                            _propertyMapping = column
                        };
                    }),
                ComplexProperties = _et.PropertyMappings
                    .Where(_pm => _pm is ComplexPropertyMapping)
                    .Cast<ComplexPropertyMapping>()
                    .Select(ExtractComplexTypeModel)
            }
            .UsingValue(_complexTypeModels.Add);
        }
    }
    
    public class ComplexTypeModel
    {
        public Type ClrType => SourceProperty?.PropertyType;
        public PropertyInfo SourceProperty { get; internal set; }

        private HashSet<ScalarPropertyModel> _props = new HashSet<ScalarPropertyModel>();
        public IEnumerable<ScalarPropertyModel> ScalarProperties
        {
            get { return _props.ToArray(); }
            internal set
            {
                _props.Clear();
                if (value != null) _props.AddRange(value);
            }
        }

        private HashSet<ComplexTypeModel> _cprops = new HashSet<ComplexTypeModel>();
        public IEnumerable<ComplexTypeModel> ComplexProperties
        {
            get { return _cprops.ToArray(); }
            internal set
            {
                _cprops.Clear();
                if (value != null) _cprops.AddRange(value);
            }
        }

        public IEnumerable<ScalarPropertyModel> AllScalarProperties => ScalarProperties.Concat(_cprops.SelectMany(_cp => _cp.AllScalarProperties));

        public override string ToString() => $"[source: {SourceProperty}, type: {ClrType.FullName}]";
    }

    public class TypeModel
    {
        private HashSet<NavigationPropertyModel> _navProps = new HashSet<NavigationPropertyModel>();
        private HashSet<ComplexTypeModel> _cprops = new HashSet<ComplexTypeModel>();
        private HashSet<ScalarPropertyModel> _props = new HashSet<ScalarPropertyModel>();
        internal string MappedTable { get; set; }
        internal EFMappings MappingRoot { get; set; }
        internal Type BaseType { get; set; }

        public TypeModel BaseTypeModel => MappingRoot.MappingFor(BaseType);

        public TableModel TableModel { get; internal set; }

        public Type ClrType { get; internal set; }

        public IEnumerable<ScalarPropertyModel> ScalarProperties
        {
            get { return _props.ToArray(); }
            internal set
            {
                _props.Clear();
                if (value != null) _props.AddRange(value);
            }
        }

        public IEnumerable<ComplexTypeModel> ComplexProperties
        {
            get { return _cprops.ToArray(); }
            internal set
            {
                _cprops.Clear();
                if (value != null) _cprops.AddRange(value);
            }
        }

        public IEnumerable<NavigationPropertyModel> NavigationProperties
        {
            get { return _navProps.ToArray(); }
            internal set
            {
                _navProps.Clear();
                if (value != null) _navProps.AddRange(value);
            }
        }
        
        public IEnumerable<ScalarPropertyModel> AllScalarProperties
        => ScalarProperties.Concat(_cprops.SelectMany(_cp => _cp.AllScalarProperties))
                           .Concat(BaseTypeModel?.AllScalarProperties ?? new ScalarPropertyModel[0]);

        public override string ToString() => $"[table: {MappedTable}, type: {ClrType.FullName}]";
    }
    public class ScalarPropertyModel
    {
        public enum KeyMode { None, StoreGenerated, SourceGenerated }

        internal EdmProperty _propertyMapping { get; set; }

        public PropertyInfo ClrProperty { get; internal set; }
        public string MappedColumn { get; internal set; }
        public bool IsForeignKey { get; internal set; }

        public bool IsKey => Key != KeyMode.None;
        public KeyMode Key { get; internal set; }


        public override bool Equals(object obj)
        => obj.Cast<ScalarPropertyModel>().Pipe(_pm => _pm?.ClrProperty == ClrProperty &&
                                                 _pm?.MappedColumn == MappedColumn);
        public override int GetHashCode() => this.PropertyHash();

        public override string ToString() => $"[clr-prop: {ClrProperty}, mapped-prop: {MappedColumn}]";
    }
    public class NavigationPropertyModel
    {
        public PropertyInfo ClrProperty { get; internal set; }

        private List<ScalarPropertyModel> _lkeys = new List<ScalarPropertyModel>();
        public IEnumerable<ScalarPropertyModel> LocalKeys
        {
            get { return _lkeys.ToArray(); }
            set
            {
                _lkeys.Clear();
                if (value != null) _lkeys.AddRange(value.UsingEach(_v => _v.IsForeignKey = true));
            }
        }

        private List<string> _exkeys = new List<string>();
        public IEnumerable<string> ExternalKeys
        {
            get { return _exkeys.ToArray(); }
            set
            {
                _exkeys.Clear();
                if (value != null) _exkeys.AddRange(value);
            }
        }
    }

    public class TableModel
    {
        public string Name { get; set; }

        private List<TypeModel> _typeModels = new List<TypeModel>();
        public IEnumerable<TypeModel> TypeModels
        {
            get { return _typeModels.ToArray(); }
            set
            {
                _typeModels.Clear();
                if (value != null) _typeModels.AddRange(value.UsingEach(_v => _v.TableModel = this));
            }
        }
        public IEnumerable<ColumnModel> ColumnModels => TypeModels.SelectMany(_tm => _tm.AllScalarProperties).Select(_sp => new ColumnModel { MappedProperty = _sp });
    }
    public class ColumnModel
    {
        public string Name => MappedProperty?._propertyMapping.Name;
        public string DbType => MappedProperty?._propertyMapping.TypeName;
        public bool IsForeignKey => MappedProperty?.IsForeignKey ?? false;
        public bool IsPrimaryKey => MappedProperty?.IsKey ?? false;

        //public bool IsIndex { get; internal set; }

        public ScalarPropertyModel MappedProperty { get; internal set; }
    }
}
