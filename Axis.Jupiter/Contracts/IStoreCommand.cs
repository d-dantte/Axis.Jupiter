using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Axis.Jupiter.Models;
using Axis.Luna.Operation;

namespace Axis.Jupiter.Contracts
{
    public interface IStoreCommand
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <param name="d"></param>
        /// <returns></returns>
        Operation<Model> Add<Model>(Model d) where Model : class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <param name="d"></param>
        /// <returns></returns>
        Operation AddBatch<Model>(IEnumerable<Model> d) where Model : class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <param name="d"></param>
        /// <returns></returns>
        Operation<Model> Update<Model>(Model d) where Model : class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <param name="d"></param>
        /// <returns></returns>
        Operation UpdateBatch<Model>(IEnumerable<Model> d) where Model : class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <param name="d"></param>
        /// <returns></returns>
        Operation<Model> Delete<Model>(Model d) where Model : class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <param name="d"></param>
        /// <returns></returns>
        Operation DeleteBatch<Model>(IEnumerable<Model> d) where Model : class;

        /// <summary>
        /// Adds a child model to the collection object of the parent's <c>collectionProperty</c> property
        /// </summary>
        /// <typeparam name="Parent"></typeparam>
        /// <typeparam name="Child"></typeparam>
        /// <param name="parent"></param>
        /// <param name="collectionPropertyExpression"></param>
        Operation AddToCollection<Parent, Child>(
            Parent parent,
            Expression<Func<Parent, ICollection<Child>>> collectionPropertyExpression, 
            params Child[] children)
            where Parent : class
            where Child : class;

        /// <summary>
        /// Adds a child model to the collection object of the parent's <c>collectionProperty</c> property
        /// </summary>
        /// <typeparam name="Parent"></typeparam>
        /// <typeparam name="Child"></typeparam>
        /// <param name="parent"></param>
        /// <param name="collectionPropertyExpression"></param>
        Operation RemoveFromCollection<Parent, Child>(
            Parent parent,
            Expression<Func<Parent, ICollection<Child>>> collectionPropertyExpression,
            params Child[] children)
            where Parent : class
            where Child : class;
    }
}