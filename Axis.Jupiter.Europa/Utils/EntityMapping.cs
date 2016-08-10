using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Axis.Jupiter.Europa.Utils
{
    /// <summary>
    /// Represents the mapping of an entitiy type to one or mode tables in the database
    ///
    /// A single entity can be mapped to more than one table when 'Entity Splitting' is used
    /// Entity Splitting involves mapping different properties from the same type to different tables
    /// See http://msdn.com/data/jj591617#2.7 for more details
    /// </summary>
    public class TypeMapping
    {
        /// <summary>
        /// The type of the entity from the model
        /// </summary>
        public Type EntityType { get; set; }

        /// <summary>
        /// The table(s) that the entity is mapped to
        /// </summary>
        internal List<TableMapping> _tableMappings { get; private set; } = new List<TableMapping>();
        public IEnumerable<TableMapping> TableMappings => _tableMappings.ToArray();

        public TableMapping Table => TableMappings.FirstOrDefault();

        public IEnumerable<PropertyInfo> Properties
            => TableMappings.FirstOrDefault()?.PropertyMappings.Select(_pm => _pm.Property) ?? new PropertyInfo[0];
    }

    /// <summary>
    /// Represents the mapping of an entity to a table in the database
    /// </summary>
    public class TableMapping
    {
        /// <summary>
        /// The name of the table the entity is mapped to
        /// </summary>
        public string TableName { get; internal set; }

        /// <summary>
        /// Details of the property-to-column mapping
        /// </summary>
        internal List<PropertyMapping> _propertyMappings { get; private set; } = new List<PropertyMapping>();

        public IEnumerable<PropertyMapping> PropertyMappings => _propertyMappings.ToArray();
    }

    /// <summary>
    /// Represents the mapping of a property to a column in the database
    /// </summary>
    public class PropertyMapping
    {
        /// <summary>
        /// The property from the entity type
        /// </summary>
        public PropertyInfo Property { get; internal set; }

        /// <summary>
        /// The column that property is mapped to
        /// </summary>
        public string ColumnName { get; internal set; }
    }

    /// <summary>
    /// Represents that mapping between entity types and tables in an EF model
    /// </summary>
    public class EFMapping
    {
        /// <summary>
        /// Mapping information for each entity type in the model
        /// </summary>
        public IEnumerable<TypeMapping> TypeMappings { get; private set; }

        public TypeMapping TypeMetadata<Entity>()// where Entity : class 
        {
            var et = typeof(Entity);
            return TypeMappings.FirstOrDefault(tm => tm.EntityType.Equals(et));
        }

        /// <summary>
        /// Initializes an instance of the EfMapping class
        /// </summary>
        /// <param name="db">The context to get the mapping from</param>
        public EFMapping(DbContext db)
        {
            var tmlist = new List<TypeMapping>();
            this.TypeMappings = tmlist;

            var metadata = ((IObjectContextAdapter)db).ObjectContext.MetadataWorkspace;

            // Conceptual part of the model has info about the shape of our entity classes
            var conceptualContainer = metadata.GetItems<EntityContainer>(DataSpace.CSpace).Single();

            // Storage part of the model has info about the shape of our tables
            var storeContainer = metadata.GetItems<EntityContainer>(DataSpace.SSpace).Single();

            // Object part of the model that contains info about the actual CLR types
            var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

            //Clr collection
            var clrCollection = metadata.GetItems<EntityType>(DataSpace.OSpace);

            // Object-Conceptual mapping part of the model that contains info about the actual CLR types
            var ocCollection = metadata.GetItemCollection(DataSpace.OCSpace);

            // Mapping part of model is not public, so we need to write to xml and use 'LINQ to XML'
            var edmx = GetEdmx(db);

            // Loop thru each entity type in the model
            foreach (var set in conceptualContainer.BaseEntitySets.OfType<EntitySet>())
            {
                var typeMapping = new TypeMapping();
                tmlist.Add(typeMapping);

                var ocmap = ocCollection.FirstOrDefault(oc => oc.conceptualIdentity() == set.ElementType.FullName);
                var clrItem = clrCollection.FirstOrDefault(ct => ct.FullName == ocmap.objectIdentity());
                typeMapping.EntityType = objectItemCollection.GetClrType(clrItem);

                //// Get the CLR type of the entity
                //typeMapping.EntityType = metadata
                //    .GetItems<EntityType>(DataSpace.OSpace)
                //    .Select(e => objectItemCollection.GetClrType(e))
                //    .SingleOrDefault(e => e.FullName == set.ElementType.FullName);

                // Get the mapping fragments for this type
                // (types may have mutliple fragments if 'Entity Splitting' is used)
                var mappingFragments = edmx
                    .Descendants()
                    .Single(e =>
                        e.Name.LocalName == "EntityTypeMapping"
                        && e.Attribute("TypeName").Value == set.ElementType.FullName)
                    .Descendants()
                    .Where(e => e.Name.LocalName == "MappingFragment");


                foreach (var mapping in mappingFragments)
                {
                    var tableMapping = new TableMapping();
                    typeMapping._tableMappings.Add(tableMapping);

                    // Find the table that this fragment maps to
                    var storeset = mapping.Attribute("StoreEntitySet").Value;
                    tableMapping.TableName = (string)storeContainer
                        .BaseEntitySets.OfType<EntitySet>()
                        .Single(s => s.Name == storeset)
                        .MetadataProperties["Table"].Value;

                    // Find the property-to-column mappings
                    var propertyMappings = mapping
                        .Descendants()
                        .Where(e => e.Name.LocalName == "ScalarProperty");

                    foreach (var propertyMapping in propertyMappings)
                    {
                        // Find the property and column being mapped
                        var propertyName = propertyMapping.Attribute("Name").Value;
                        var columnName = propertyMapping.Attribute("ColumnName").Value;

                        tableMapping._propertyMappings.Add(new PropertyMapping
                        {
                            Property = typeMapping.EntityType.GetProperty(propertyName),
                            ColumnName = columnName
                        });
                    }
                }
            }
        }

        private static XDocument GetEdmx(DbContext db)
        {
            XDocument doc;
            using (var memoryStream = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(memoryStream, new XmlWriterSettings { Indent = true }))
                {
                    EdmxWriter.WriteEdmx(db, xmlWriter);
                }

                memoryStream.Position = 0;

                doc = XDocument.Load(memoryStream);
            }
            return doc;
        }
    }

    public static class __MetadataExtensions
    {
        public static string conceptualIdentity(this GlobalItem gitem)
        {
            var parts = gitem.ToString().Split(':');
            return parts[1];
        }
        public static string objectIdentity(this GlobalItem gitem)
        {
            var parts = gitem.ToString().Split(':');
            return parts[0];
        }
    }
}
