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
        private DataStore _store = null;

        internal ModelConverter(DataStore store)
        {
            _store = store.ThrowIfNull("invlid data store supplied");
            store.ContextConfig.ConfiguredModules
                .Select(_m => _m as IEntityMapConfigProvider)
                .SelectMany(_next => _next.ConfiguredEntityMaps())
                .Pipe(_mapConfigCache.AddRange);
        }

        public object ToEntity<Model>(Model model)
        {
            //TODO: Check in the context's local cache for entities before creating new ones with the activator
            if (model == null) return null;
            else 
            {
                if (_conversionContext.ContainsKey(model)) return _conversionContext[model];
                else
                {
                    var mapConfig = _mapConfigCache
                        .FirstOrDefault(_next => _next.ModelType == typeof(Model))
                        .ThrowIfNull($"No Map configuration found for model-type: {typeof(Model)}");

                    var entityType = mapConfig.EntityType;
                    var local = _store.GetLocally(_store.Set(entityType), entityType, model); //<-- RELIES on the model and entity having identical property names - 


                    var entity = Activator.CreateInstance(entityType);
                    //mapConfig.ModelToEntityMapper(this, model, entity); //<-- map


                    if (local == null) return _conversionContext[model] = entity;
                    else return _conversionContext[model] = local;
                }
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
                    var mapConfig = _mapConfigCache
                       .FirstOrDefault(_next => _next.ModelType == typeof(Model))
                       .ThrowIfNull($"No Map configuration found for model-type: {typeof(Model)}");

                    var modleType = mapConfig.ModelType;
                    var model = Activator.CreateInstance(modleType);
                    //mapConfig.EntityToModelMapper(this, entity, model); //<-- map

                    //map the entity
                    //mapConfig.EntityToModelMapper(this, model);
                }

                return (Model)entity;
            }
        }
    }
}
