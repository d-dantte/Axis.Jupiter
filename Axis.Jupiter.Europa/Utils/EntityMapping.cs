using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

using static Axis.Luna.Extensions.ObjectExtensions;

namespace Axis.Jupiter.Europa.Utils
{
    //public static class MetadataExtensions
    //{
    //    public static XDocument GetEdmx(this DbModel dbm)
    //    {
    //        XDocument doc;
    //        using (var memoryStream = new MemoryStream())
    //        {
    //            using (var xmlWriter = XmlWriter.Create(memoryStream, new XmlWriterSettings { Indent = true }))
    //            {
    //                EdmxWriter.WriteEdmx(dbm, xmlWriter);
    //            }

    //            memoryStream.Position = 0;

    //            doc = XDocument.Load(memoryStream);
    //        }
    //        return doc;
    //    }
    //}

    public class EFMappings
    {
        public EFMappings(DbModel model)
        {
            #region Complex Mappings
            //here we map only the complex type. Their properties get mapped when we walk the conceptual-to-store mappings
            model.ConceptualModel
                 .ComplexTypes
                 .Select(_ct =>
                 {
                     return new ComplexTypeMapping
                     {
                         ClrType = _ct.MetadataProperties
                            .FirstOrDefault(MetadataClrTypePredicate).Value
                            .Cast<Type>()
                     };
                 })
                 .ForAll(_ctm => _complexTypeMappings.Add(_ctm.ClrType, _ctm));
            #endregion

            #region Type Mappings
            model.ConceptualToStoreMapping
                 .EntitySetMappings
                 .SelectMany(_esm => _esm.EntityTypeMappings.SelectMany(_etm => _etm.Fragments.Select(_ef =>
                 {
                     var clrType = _etm.EntityType
                         .MetadataProperties
                         .FirstOrDefault(MetadataClrTypePredicate).Value
                         .Cast<Type>(); //<-- as Type

                     var typeModel = new TypeMapping
                     {
                         MappingRoot = this,
                         ClrType = clrType,
                         BaseType = _etm.EntityType.BaseType?.MetadataProperties
                            .FirstOrDefault(MetadataClrTypePredicate).Value?
                            .Cast<Type>(),
                         MappedTable = _ef.StoreEntitySet.Table
                     };

                     //scalar properties
                     typeModel.ScalarProperties = _ef.PropertyMappings
                        .Where(_pm => _pm is System.Data.Entity.Core.Mapping.ScalarPropertyMapping)
                        .Where(_pm => _pm.Property.DeclaringType == _etm.EntityType)
                        .Select(_pm =>
                        {
                            var column = _pm.Cast<System.Data.Entity.Core.Mapping.ScalarPropertyMapping>().Column;
                            return new ScalarPropertyMapping
                            {
                                Owner = typeModel,
                                ClrProperty = clrType.GetProperty(_pm.Property.Name),
                                MappedColumn = column.Name,
                                Key = column.IsStoreGeneratedIdentity ? ScalarPropertyMapping.KeyMode.StoreGenerated :
                                      column.DeclaringType.Cast<EntityType>().KeyProperties.Contains(column) ? ScalarPropertyMapping.KeyMode.SourceGenerated :
                                      ScalarPropertyMapping.KeyMode.None,
                                _propertyMapping = column
                            };
                        });

                     //complex properties
                     typeModel.ComplexProperties = _ef.PropertyMappings
                        .Where(_pm => _pm is System.Data.Entity.Core.Mapping.ComplexPropertyMapping)
                        .Cast<System.Data.Entity.Core.Mapping.ComplexPropertyMapping>()
                        .Select(_cpm => ExtractComplexTypeModel(typeModel, _cpm));

                     //navigation properties
                     typeModel.NavigationProperties = _etm.EntityType.NavigationProperties
                        .Where(_np => _np.DeclaringType == _etm.EntityType)
                        .Select(_np =>
                        {
                            var association = model.ConceptualModel.AssociationTypes.FirstOrDefault(_ass => _ass.Name == _np.RelationshipType.Name);
                            var isSource = association.Constraint.FromRole.TypeUsage.EdmType.Name.Contains(_etm.EntityType.Name);
                            var constraint = _np.RelationshipType.Cast<AssociationType>().Constraint;
                            return new NavigationPropertyMapping
                            {
                                ClrProperty = clrType.GetProperty(_np.Name),
                                LocalKeys = isSource ?
                                            typeModel.ScalarProperties.Where(_sp => constraint.FromProperties.Any(_p => _p.Name == _sp.ClrProperty.Name)) :
                                            typeModel.ScalarProperties.Where(_sp => constraint.ToProperties.Any(_p => _p.Name == _sp.ClrProperty.Name)),
                                ExternalKeys = !isSource ?
                                               constraint.FromProperties.Select(_p => _p.Name) :
                                               constraint.ToProperties.Select(_p => _p.Name)
                            };
                        });

                     return typeModel;
                 })))
                 .ForAll(_tm => _typeMappings.Add(_tm.ClrType, _tm));
            #endregion

            #region Table Mappings
            _typeMappings.Values.GroupBy(_tm => _tm.MappedTable)
                 .Select(_group =>
                 {
                     return new TableMapping
                     {
                         Name = _group.Key,
                         TypeModels = _group
                     };
                 })
                 .ForAll(_tableMappings.Add); //<-- ToList().ForEach(...)
            #endregion
        }

        private Dictionary<Type, TypeMapping> _typeMappings = new Dictionary<Type, TypeMapping>();
        private Dictionary<Type, ComplexTypeMapping> _complexTypeMappings = new Dictionary<Type, ComplexTypeMapping>();
        private List<TableMapping> _tableMappings = new List<TableMapping>();

        public IEnumerable<IMapping> Mappings => TypeMappings.Cast<IMapping>().Concat(ComplexMappings);
        public IEnumerable<TypeMapping> TypeMappings => _typeMappings.Values.ToArray();
        public IEnumerable<ComplexTypeMapping> ComplexMappings => _complexTypeMappings.Values.ToArray();
        public IEnumerable<TableMapping> TableMappings => _tableMappings.ToArray();

        public IMapping MappingFor<Entity>() => MappingFor(typeof(Entity));
        public IMapping MappingFor(Type entityType) => _typeMappings.ContainsKey(entityType) ? _typeMappings[entityType].Cast<IMapping>() :
                                                       _complexTypeMappings.ContainsKey(entityType) ? _complexTypeMappings[entityType].Cast<IMapping>() :
                                                       null;

        public TypeMapping TypeMappingFor<Entity>() => TypeMappingFor(typeof(Entity));
        public TypeMapping TypeMappingFor(Type type)
        {
            if (type == null) return null;
            else return _typeMappings.ContainsKey(type) ?
                        _typeMappings[type] : null;
        }

        public ComplexTypeMapping ComplexTypeMappingFor<Entity>() => ComplexTypeMappingFor(typeof(Entity));
        public ComplexTypeMapping ComplexTypeMappingFor(Type type) => _complexTypeMappings.ContainsKey(type)?
                                                                      _complexTypeMappings[type]: null;

        public IEnumerable<TableMapping> TableMappingsFor<Entity>() => TableMappingsFor(typeof(Entity));
        public IEnumerable<TableMapping> TableMappingsFor(Type entityType) => _tableMappings.Where(_tm => _tm.TypeModels.Any(_tym => _tym.ClrType == entityType));

        private ComplexPropertyMapping ExtractComplexTypeModel(IComplexPropertyContainer owner, System.Data.Entity.Core.Mapping.ComplexPropertyMapping mapping)
        {
            var clrProp = mapping.Property.MetadataProperties["ClrPropertyInfo"].Value as PropertyInfo;
            var complexTypeMapping = ComplexTypeMappingFor(clrProp.PropertyType);

            //map complex type properties if needed
            if(complexTypeMapping.ComplexProperties.Count() == 0)
            {
                var _et = mapping.TypeMappings.FirstOrDefault();

                complexTypeMapping.ScalarProperties = _et.PropertyMappings
                    .Where(_pm => _pm is System.Data.Entity.Core.Mapping.ScalarPropertyMapping)
                    .Where(_pm => clrProp.PropertyType.GetProperty(_pm.Property.Name).DeclaringType == clrProp.PropertyType)
                    .Select(_pm =>
                    {
                        var column = _pm.Cast<System.Data.Entity.Core.Mapping.ScalarPropertyMapping>().Column;
                        return new ScalarPropertyMapping
                        {
                            Owner = complexTypeMapping,
                            ClrProperty = clrProp.PropertyType.GetProperty(_pm.Property.Name),
                            MappedColumn = column.Name,
                            Key = column.IsStoreGeneratedIdentity ? ScalarPropertyMapping.KeyMode.StoreGenerated :
                                  column.DeclaringType.Cast<EntityType>().KeyProperties.Contains(column) ? ScalarPropertyMapping.KeyMode.SourceGenerated :
                                  ScalarPropertyMapping.KeyMode.None,
                            _propertyMapping = column
                        };
                    });

                complexTypeMapping.ComplexProperties = _et.PropertyMappings
                    .Where(_pm => _pm is System.Data.Entity.Core.Mapping.ComplexPropertyMapping)
                    .Cast<System.Data.Entity.Core.Mapping.ComplexPropertyMapping>()
                    .Select(_cpm => ExtractComplexTypeModel(complexTypeMapping, _cpm));
            }

            return new ComplexPropertyMapping
            {
                ClrProperty = clrProp,
                Owner = owner,
                ComplexTypeRef = complexTypeMapping
            };
        }
        private bool MetadataClrTypePredicate(MetadataProperty metadataProperty)
        => metadataProperty.Name == "http://schemas.microsoft.com/ado/2013/11/edm/customannotation:ClrType";
    }

    public class ScalarPropertyMapping
    {
        public enum KeyMode { None, StoreGenerated, SourceGenerated }

        internal EdmProperty _propertyMapping { get; set; }

        public IScalarPropertyContainer Owner { get; set; }

        public PropertyInfo ClrProperty { get; internal set; }
        public string MappedColumn { get; internal set; }
        public bool IsForeignKey { get; internal set; }
        public bool IsKey => Key != KeyMode.None;
        public KeyMode Key { get; internal set; }
        

        public override string ToString()
        => $"[{Owner?.GetType().MinimalAQName()}].{ClrProperty.Name} -> {MappedColumn}";
        public override int GetHashCode()
        => ValueHash(new object[] { ClrProperty, ClrProperty, MappedColumn, Key });
        public override bool Equals(object obj)
        {
            var _obj = obj as ScalarPropertyMapping;
            return _obj != null &&
                   _obj.GetHashCode() == GetHashCode() &&
                   _obj.ClrProperty == ClrProperty &&
                   _obj.MappedColumn == MappedColumn &&
                   _obj.Key == Key;
        }
    }
    public class NavigationPropertyMapping
    {
        private List<string> _exkeys = new List<string>();
        private List<ScalarPropertyMapping> _lkeys = new List<ScalarPropertyMapping>();

        public INavigationPropertyContainer Owner { get; internal set; }

        public PropertyInfo ClrProperty { get; internal set; }
        public IEnumerable<ScalarPropertyMapping> LocalKeys
        {
            get { return _lkeys.ToArray(); }
            set
            {
                _lkeys.Clear();
                if (value != null) _lkeys.AddRange(value.UsingEach(_v => _v.IsForeignKey = true));
            }
        }
        public IEnumerable<string> ExternalKeys
        {
            get { return _exkeys.ToArray(); }
            set
            {
                _exkeys.Clear();
                if (value != null) _exkeys.AddRange(value);
            }
        }

        public override string ToString() 
        => $"[{Owner?.GetType().MinimalAQName()}].{ClrProperty.Name} -> {_lkeys.Select(_lk=> _lk.MappedColumn).JoinUsing(",")}";
        public override int GetHashCode() 
        => ValueHash(new object[] { ClrProperty, LocalKeys, ExternalKeys });
        public override bool Equals(object obj)
        {
            var _obj = obj as NavigationPropertyMapping;
            return _obj != null &&
                   _obj.GetHashCode() == GetHashCode() &&
                   _obj.ClrProperty == ClrProperty &&
                   _obj.LocalKeys.SequenceEqual(LocalKeys) &&
                   _obj.ExternalKeys.SequenceEqual(ExternalKeys);
        }
    }
    public class ComplexPropertyMapping
    {
        public IComplexPropertyContainer Owner { get; internal set; }

        public PropertyInfo ClrProperty { get; internal set; }
        public ComplexTypeMapping ComplexTypeRef { get; internal set; }


        public override string ToString() 
        => $"[{Owner?.GetType().MinimalAQName()}].{ClrProperty.Name} -> {ComplexTypeRef.AllScalarProperties.Select(_sp => _sp.MappedColumn).JoinUsing(",")}";
        public override int GetHashCode() => ValueHash(new object[] { ClrProperty });
        public override bool Equals(object obj)
        {
            var _obj = obj as ComplexPropertyMapping;
            return _obj != null &&
                   _obj.GetHashCode() == GetHashCode() &&
                   _obj.ClrProperty == ClrProperty;
        }
    }


    public interface IScalarPropertyContainer
    {
        IEnumerable<ScalarPropertyMapping> ScalarProperties { get; }
        ScalarPropertyMapping GetScalarProperty(string name);
    }
    public interface INavigationPropertyContainer
    {
        IEnumerable<NavigationPropertyMapping> NavigationProperties { get; }
        NavigationPropertyMapping GetNavigationProperty(string name);
    }
    public interface IComplexPropertyContainer
    {
        IEnumerable<ComplexPropertyMapping> ComplexProperties { get; }
        ComplexPropertyMapping GetComplexProperty(string name);
    }

    public interface IMapping: IScalarPropertyContainer, IComplexPropertyContainer
    {
        Type ClrType { get; }
    }


    public class ComplexTypeMapping: IMapping
    {
        private HashSet<ComplexPropertyMapping> _cprops = new HashSet<ComplexPropertyMapping>();
        private HashSet<ScalarPropertyMapping> _props = new HashSet<ScalarPropertyMapping>();

        public Type ClrType { get; internal set; }

        public IEnumerable<ScalarPropertyMapping> ScalarProperties
        {
            get { return _props.ToArray(); }
            internal set
            {
                _props.Clear();
                if (value != null) _props.AddRange(value);
            }
        }
        public IEnumerable<ComplexPropertyMapping> ComplexProperties
        {
            get { return _cprops.ToArray(); }
            internal set
            {
                _cprops.Clear();
                if (value != null) _cprops.AddRange(value);
            }
        }
        public IEnumerable<ScalarPropertyMapping> AllScalarProperties => ScalarProperties.Concat(_cprops.SelectMany(_cp => _cp.ComplexTypeRef.AllScalarProperties));

        public override string ToString() => $"[{ClrType.FullName}]";

        public ComplexPropertyMapping GetComplexProperty(string name)
        => _cprops.FirstOrDefault(_cprop => _cprop.ClrProperty.Name == name);

        public ScalarPropertyMapping GetScalarProperty(string name)
        => _props.FirstOrDefault(_prop => _prop.ClrProperty.Name == name);
    }

    public class TypeMapping: IMapping, INavigationPropertyContainer
    {
        private HashSet<NavigationPropertyMapping> _navProps = new HashSet<NavigationPropertyMapping>();
        private HashSet<ComplexPropertyMapping> _cprops = new HashSet<ComplexPropertyMapping>();
        private HashSet<ScalarPropertyMapping> _props = new HashSet<ScalarPropertyMapping>();
        internal string MappedTable { get; set; }
        internal EFMappings MappingRoot { get; set; }
        internal Type BaseType { get; set; }

        public TypeMapping BaseTypeMapping => MappingRoot.TypeMappingFor(BaseType);

        public TableMapping TableModel { get; internal set; }

        public Type ClrType { get; internal set; }

        public IEnumerable<ScalarPropertyMapping> ScalarProperties
        {
            get { return _props.ToArray(); }
            internal set
            {
                _props.Clear();
                if (value != null) _props.AddRange(value);
            }
        }

        public IEnumerable<ComplexPropertyMapping> ComplexProperties
        {
            get { return _cprops.ToArray(); }
            internal set
            {
                _cprops.Clear();
                if (value != null) _cprops.AddRange(value);
            }
        }

        public IEnumerable<NavigationPropertyMapping> NavigationProperties
        {
            get { return _navProps.ToArray(); }
            internal set
            {
                _navProps.Clear();
                if (value != null) _navProps.AddRange(value);
            }
        }
        
        public IEnumerable<ScalarPropertyMapping> AllScalarProperties
        => ScalarProperties.Concat(_cprops.SelectMany(_cp => _cp.ComplexTypeRef.AllScalarProperties))
                           .Concat(BaseTypeMapping?.AllScalarProperties ?? new ScalarPropertyMapping[0]);


        public ComplexPropertyMapping GetComplexProperty(string name)
        => _cprops.FirstOrDefault(_cprop => _cprop.ClrProperty.Name == name);

        public ScalarPropertyMapping GetScalarProperty(string name)
        => _props.FirstOrDefault(_prop => _prop.ClrProperty.Name == name);

        public NavigationPropertyMapping GetNavigationProperty(string name)
        => _navProps.FirstOrDefault(_prop => _prop.ClrProperty.Name == name);


        public override string ToString() => $"[table: {MappedTable}, type: {ClrType.FullName}]";
    }

    public class TableMapping
    {
        public string Name { get; set; }

        private List<TypeMapping> _typeModels = new List<TypeMapping>();
        public IEnumerable<TypeMapping> TypeModels
        {
            get { return _typeModels.ToArray(); }
            set
            {
                _typeModels.Clear();
                if (value != null) _typeModels.AddRange(value.UsingEach(_v => _v.TableModel = this));
            }
        }
        public IEnumerable<ColumnMapping> ColumnModels => TypeModels.SelectMany(_tm => _tm.AllScalarProperties).Select(_sp => new ColumnMapping { MappedProperty = _sp });
    }

    public class ColumnMapping
    {
        public string Name => MappedProperty?._propertyMapping.Name;
        public string DbType => MappedProperty?._propertyMapping.TypeName;
        public bool IsForeignKey => MappedProperty?.IsForeignKey ?? false;
        public bool IsPrimaryKey => MappedProperty?.IsKey ?? false;

        //public bool IsIndex { get; internal set; }

        public ScalarPropertyMapping MappedProperty { get; internal set; }
    }
}
