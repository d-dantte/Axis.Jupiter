using System;
using System.Collections.Generic;
using System.Linq;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Models;
using Axis.Jupiter.Services;
using Axis.Luna.Operation;
using Microsoft.EntityFrameworkCore;
using Axis.Luna.Extensions;

namespace Axis.Jupiter.EFCore
{
    public interface IEFStoreCommand: IStoreCommand
    {
        DbContext EFContext { get; }
    }

    public class EFStoreCommand: IEFStoreCommand
    {
        private readonly DbContext _context;
        private readonly TypeTransformer _transformer;

        public DbContext EFContext => _context;


        public EFStoreCommand(TypeTransformer transformer, DbContext context)
        {
            _context = context ?? throw new Exception("Invalid Context specified: null");
            _transformer = transformer ?? throw new Exception("Invalid Model Transformer specified: null");
        }

        public Operation<Model> Add<Model>(Model model) 
        where Model : class => Operation.Try(async () =>
        {
            var graph = _transformer.ToEntity(model, TransformCommand.Add);

            //validate
            graph
                .ThrowIfNull(new Exception("Invalid entity list"));

            //store the entity first
            await AddEntityGraphs(graph);

            return _transformer.ToModel<Model>(graph.Entity, TransformCommand.Add);
        });

        public Operation AddBatch<Model>(IEnumerable<Model> models)
        where Model : class => Operation.Try(async () =>
        {
            var graphs = models
                .Select(model => _transformer.ToEntity(model, TransformCommand.Add))
                .ThrowIf(ContainsNull, new Exception("Invalid Graph List: one of the object-transformation returned null"));

            await AddEntityGraphs(graphs.ToArray());
        });


        public Operation<Model> Update<Model>(Model model)
        where Model : class => Operation.Try(async () =>
        {
            var entity = _transformer.ToEntity(model, TransformCommand.Update);
            var entry = _context.Update(entity);

            await _context.SaveChangesAsync();

            return _transformer.ToModel<Model>(entry.Entity, TransformCommand.Update);
        });

        public Operation UpdateBatch<Model>(IEnumerable<Model> models)
        where Model : class => Operation.Try(async () =>
        {
            var entities = models.Select(model => _transformer.ToEntity(model, TransformCommand.Update));
            _context.UpdateRange(entities);

            await _context.SaveChangesAsync();
        });


        public Operation<Model> Delete<Model>(Model model)
        where Model : class => Operation.Try(async () =>
        {
            var entity = _transformer.ToEntity(model, TransformCommand.Update);
            var entry = _context.Remove(entity);

            await _context.SaveChangesAsync();

            return _transformer.ToModel<Model>(entry.Entity, TransformCommand.Remove);
        });

        public Operation DeleteBatch<Model>(IEnumerable<Model> models)
        where Model : class => Operation.Try(async () =>
        {
            var entities = models.Select(model => _transformer.ToEntity(model, TransformCommand.Remove));
            _context.RemoveRange(entities);

            await _context.SaveChangesAsync();
        });


        public Operation<Child[]> AddToCollection<Parent, Child>(
            Parent parent,
            string collectionProperty,
            Child[] children)
            where Parent : class
            where Child : class => Operation.Try(() =>
        {
            throw new NotImplementedException();
        });

        public Operation<Child[]> RemoveFromCollection<Parent, Child>(
            Parent parent,
            string collectionProperty,
            Child[] children)
            where Parent : class
            where Child : class => Operation.Try(() =>
        {
            throw new NotImplementedException();
        });


        /// <summary>
        /// Using the TypeTransformer, return an EntityGraph object that represents all the distinct objects that would be added to the
        /// database. Rules governing how the EntityGraph is treated are as follows:
        /// 1. Save the EntityGraph.Entity.
        /// 2. Run through all Secondary EntityRefs, call the Ref.BindId to assign the new Id gotten in step 1.
        /// 3. Run through all ListRefs, call Ref.BindId to assign the new Id gotten in step 1.
        /// 4. Gather the EntityGraphs from 2 & 3 and Recursively call <c>AddEntityGraphs</c> on the combination of them.
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        private Operation AddEntityGraphs(params EntityGraph[] graphs)
        => Operation.Try(async () =>
        {
            await graphs
                .Select(graph => graph.Entity)
                .ToArray()
                .Pipe(_context.AddRangeAsync);

            await _context.SaveChangesAsync(true);

            //get all secondary entity-refs
            var refNodes = graphs
                .SelectMany(graph => graph.EntityRefs
                .Where(IsSecondaryRef))
                .Select(@ref =>
                {
                    @ref.BindId.Invoke();
                    return @ref.Ref;
                })
                .ToList();

            //get all list-refs
            graphs
                .SelectMany(graph => graph.ListRefs)
                .SelectMany(list =>
                {
                    list.BindId.Invoke();
                    return list.Entities;
                })
                .Pipe(refNodes.AddRange);

            if (refNodes.Count > 0)
                await AddEntityGraphs(refNodes.ToArray());
        });


        private static bool IsSecondaryRef(EntityRef @ref) => @ref?.RefType == RefType.Secondary;

        private static bool ContainsNull(IEnumerable<EntityGraph> graphs)  => graphs?.Any(graph => graph == null) == true;
    }
}
