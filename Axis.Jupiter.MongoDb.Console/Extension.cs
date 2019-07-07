using Axis.Jupiter.MongoDb.ConsoleTest.Entities;
using Axis.Jupiter.MongoDb.ConsoleTest.Models;

namespace Axis.Jupiter.MongoDb.ConsoleTest
{
    public static class Extension
    {
        public static Entity CopyBase<Key, Entity>(this BaseModel<Key> model, Entity entity)
        where Entity: BaseEntity<Key>
        {
            entity.CreatedBy = model.CreatedBy;
            entity.CreatedOn = model.CreatedOn;
            entity.Key = model.Id;

            return entity;
        }

        public static Model CopyBase<Key, Model>(this BaseEntity<Key> entity, Model model)
        where Model: BaseModel<Key>
        {
            model.CreatedBy = entity.CreatedBy;
            model.CreatedOn = entity.CreatedOn;
            model.Id = entity.Key;

            return model;
        }
    }
}
