using Axis.Jupiter.Europa.Mappings;
using Axis.Jupiter.Europa.Module;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Jupiter.Europa
{
    public class ModelConverter
    {
        private List<IEntityMapConfiguration> _mapConfigCache = new List<IEntityMapConfiguration>();
        private Dictionary<object, object> _conversionContext = new Dictionary<object, object>();

        public ModelConverter(IEnumerable<IModuleConfigProvider> modules)
        {
            modules.SelectMany(_next => _next.ConfiguredEntityMaps())
                   .Pipe(_mapConfigCache.AddRange);
        }

        public object ToEntity<Model>(Model model)
        {
            if (model == null) return null;
            else 
            {
                if(!_conversionContext.ContainsKey(model))
                {
                    var mapConfig = _mapConfigCache.First(_next => _next.ModelType == typeof(Model));
                    var entityType = mapConfig.EntityType;
                    var entity = Activator.CreateInstance(entityType);
                    _conversionContext.Add(model, entity);

                    //map the entity
                    mapConfig.ModelToEntity.Invoke(this, model, entity);
                }

                return _conversionContext[model];
            }
        }

        public Model ToModel<Model>(object entity)
        where Model: class
        {
            if (entity == null) return null;
            else
            {
                if (!_conversionContext.ContainsKey(entity))
                {
                    var mapConfig = _mapConfigCache.First(_next => _next.ModelType == typeof(Model));
                    _conversionContext.Add(entity, entity);

                    //map the entity
                    mapConfig.EntityToModel.Invoke(this, entity);
                }

                return (Model)entity;
            }
        }
    }
}
