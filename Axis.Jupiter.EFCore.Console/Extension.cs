using Axis.Jupiter.EFCore.ConsoleTest.Entities;
using Axis.Jupiter.EFCore.ConsoleTest.Models;

namespace Axis.Jupiter.EFCore.ConsoleTest
{
    public static class Extension
    {
        public static Entity CopyTo<Key, Entity>(this BaseModel<Key> model, Entity entity)
        where Entity: BaseEntity<Key>
        {
            entity.CreatedBy = model.CreatedBy;
            entity.CreatedOn = model.CreatedOn;
            entity.Id = model.Id;

            return entity;
        }

        public static Model CopyTo<Key, Model>(this BaseEntity<Key> entity, Model model)
        where Model: BaseModel<Key>
        {
            model.CreatedBy = entity.CreatedBy;
            model.CreatedOn = entity.CreatedOn;
            model.Id = entity.Id;

            return model;
        }
    }
}
