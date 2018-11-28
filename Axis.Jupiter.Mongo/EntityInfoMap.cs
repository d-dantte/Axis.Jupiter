using System;
using System.Collections.Generic;

namespace Axis.Jupiter.Mongo
{
    public class EntityInfoMap
    {
        private readonly Dictionary<Type, EntityInfo> _infoMaps = new Dictionary<Type, EntityInfo>();


        public EntityInfoMap AddInfo(EntityInfo info)
        {
            if (info == null)
                throw new Exception("Invalid Entity Info provided: null");

            _infoMaps[info.EntityType] = info;

            return this;
        }

        public EntityInfo InfoFor(Type type) => _infoMaps[type];

        public EntityInfo InfoFor<Type>() => _infoMaps[typeof(Type)];
    }
}