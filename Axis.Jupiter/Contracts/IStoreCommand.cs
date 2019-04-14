using System.Collections.Generic;
using Axis.Luna.Operation;

namespace Axis.Jupiter.Contracts
{
    public interface IStoreCommand
    {
        Operation<Model> Add<Model>(Model d) where Model : class;
        Operation AddBatch<Model>(IEnumerable<Model> d) where Model : class;

        Operation<Model> Update<Model>(Model d) where Model : class;
        Operation UpdateBatch<Model>(IEnumerable<Model> d) where Model : class;

        Operation<Model> Delete<Model>(Model d) where Model : class;
        Operation DeleteBatch<Model>(IEnumerable<Model> d) where Model : class;

        /// <summary>
        /// Adds a child model to the collection object of the parent's <c>collectionProperty</c> property
        /// </summary>
        /// <typeparam name="Parent"></typeparam>
        /// <typeparam name="Child"></typeparam>
        /// <param name="parent"></param>
        /// <param name="collectionProperty"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        Operation<Child[]> AddToCollection<Parent, Child>(
            Parent parent, 
            string collectionProperty,
            Child[] children)
            where Parent : class
            where Child : class;

        /// <summary>
        /// Removs a child model from the collection object of the parent's <c>collectionProperty</c> property
        /// </summary>
        /// <typeparam name="Parent"></typeparam>
        /// <typeparam name="Child"></typeparam>
        /// <param name="parent"></param>
        /// <param name="collectionProperty"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        Operation<Child[]> RemoveFromCollection<Parent, Child>(
            Parent parent, 
            string collectionProperty,
            Child[] children)
            where Parent : class
            where Child : class;
    }
}