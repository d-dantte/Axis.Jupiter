using Axis.Jupiter.Europa.Mappings;
using Axis.Jupiter.Europa.Module;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Jupiter.Europa
{
    //public class _ModelConverter
    //{
    //    private Dictionary<Type, IEntityMapConfiguration> _mapConfigCache = new Dictionary<Type, IEntityMapConfiguration>();
    //    private Dictionary<object, object> _conversionContext = new Dictionary<object, object>();
    //    private DataStore _store = null;

    //    public _ModelConverter(DataStore store)
    //    {
    //        _store = store.ThrowIfNull("invlid data store supplied");
    //        store.ContextConfig.ConfiguredModules
    //            .Select(_m => _m as IEntityMapConfigProvider)
    //            .SelectMany(_next => _next.ConfiguredEntityMaps())
    //            .Select(_map => _map.ModelType.ValuePair(_map))
    //            .ForAll(_kvp => _mapConfigCache.Add(_kvp.Key, _kvp.Value));
    //    }

    //    public object ToEntity<Model>(Model model)
    //    {
    //        if (model == null) return null;
    //        else if (_conversionContext.ContainsKey(model)) return _conversionContext[model];
    //        else
    //        {
    //            var mapconfig = _mapConfigCache[typeof(Model)];
    //            var entity = _conversionContext[model] = Activator.CreateInstance(mapconfig.EntityType);

    //            mapconfig.CopyToEntity(model, entity, this);

    //            return entity;
    //        }
    //    }

    //    public Model ToModel<Model>(object entity)
    //    where Model : class
    //    {
    //        if (entity == null) return null;
    //        else if (_conversionContext.ContainsKey(entity)) return (Model)_conversionContext[entity];
    //        else
    //        {
    //            var mapconfig = _mapConfigCache[typeof(Model)];
    //            var model = _conversionContext[entity] = Activator.CreateInstance<Model>();

    //            mapconfig.CopyToModel(entity, model, this);

    //            return (Model)model;
    //        }
    //    }
    //}


    public class ModelConverter
    {
        private Dictionary<Type, IEntityMapConfiguration[]> _mapConfigCache = new Dictionary<Type, IEntityMapConfiguration[]>();
        private Dictionary<object, object> _conversionContext = new Dictionary<object, object>();
        private DataStore _store = null;

        public ModelConverter(DataStore store)
        {
            _store = store.ThrowIfNull("invlid data store supplied");
            store.ContextConfig.ConfiguredModules
                .Select(_m => _m as IEntityMapConfigProvider)
                .SelectMany(_next => _next.ConfiguredEntityMaps())
                .GroupBy(_map => _map.ModelType)
                .ForAll(_groups => _mapConfigCache.Add(_groups.Key, _groups.ToArray()));
        }

        public object ToEntity<Model>(Model model)
        where Model: class
        {
            if (model == null) return null;
            else if (_conversionContext.ContainsKey(model)) return _conversionContext[model];
            else
            {
                var mapconfig = _mapConfigCache[typeof(Model)]
                    .FirstOrDefault()
                    .ThrowIfNull("No Map Configuration found");

                var entity = _conversionContext[model] = mapconfig.EntityFactory?
                    .Invoke(model) ??
                    Activator.CreateInstance(mapconfig.EntityType);

                mapconfig.CopyToEntity(model, entity, this);

                return entity;
            }
        }
        public Entity ToEntity<Model, Entity>(Model model)
        where Model: class
        where Entity: class
        {
            if (model == null) return null;
            else if (_conversionContext.ContainsKey(model)) return (Entity)_conversionContext[model];
            else
            {
                var mapconfig = _mapConfigCache[typeof(Model)]
                    .FirstOrDefault(_map => _map.EntityType == typeof(Entity))
                    .ThrowIfNull("No Map Configuration found");

                var entity = mapconfig.EntityFactory?
                    .Invoke(model) as Entity ??
                    Activator.CreateInstance<Entity>();

                _conversionContext[model] = entity;

                mapconfig.CopyToEntity(model, entity, this);

                return entity;
            }
        }

        public Model ToModel<Model>(object entity)
        where Model : class
        {
            if (entity == null) return null;
            else if (_conversionContext.ContainsKey(entity)) return (Model)_conversionContext[entity];
            else
            {
                var mapconfig = _mapConfigCache[typeof(Model)]
                    .FirstOrDefault()
                    .ThrowIfNull("No Map Configuration found");

                var model = _conversionContext[entity] = mapconfig.ModelFactory?
                    .Invoke(entity) ??
                    Activator.CreateInstance<Model>();

                mapconfig.CopyToModel(entity, model, this);

                return (Model)model;
            }
        }
        public Model ToModel<Entity, Model>(Entity entity)
        where Entity : class
        where Model : class
        {
            if (entity == null) return null;
            else if (_conversionContext.ContainsKey(entity)) return (Model)_conversionContext[entity];
            else
            {
                var mapconfig = _mapConfigCache[typeof(Model)]
                    .FirstOrDefault(_map => _map.EntityType == typeof(Entity))
                    .ThrowIfNull("No Map Configuration found");

                var model = mapconfig.ModelFactory?
                    .Invoke(entity) as Model ??
                    Activator.CreateInstance<Model>();

                _conversionContext[entity] = model;

                mapconfig.CopyToModel(entity, model, this);

                return model;
            }
        }
    }
}