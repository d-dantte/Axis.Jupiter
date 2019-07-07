using System;

namespace Axis.Jupiter.Models
{
    public enum RefInfoResult
    {
        None,
        Model,
        Entity
    }

    /// <summary>
    /// 
    /// </summary>
    public class CollectionRefInfo
    {
        public object Entity { get; }

        public object Model { get; }

        public CollectionRefCommand Command { get; }

        public int Rank { get; }

        public RefInfoResult Result { get; }

        public CollectionRefInfo(
            object model,
            object entity, 
            CollectionRefCommand command,
            int rank = 0,
            RefInfoResult result = RefInfoResult.None)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));

            Entity = entity ?? throw new ArgumentNullException(nameof(entity));

            Command = command;

            Rank = rank;

            Result = result;
        }
    }
}
