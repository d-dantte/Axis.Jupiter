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
            var entity = _transformer.ToEntity(model, TransformCommand.Add);
            entity = await _context.AddAsync(entity);

            await _context.SaveChangesAsync();

            return _transformer.ToModel<Model>(entity, TransformCommand.Add);
        });

        public Operation AddBatch<Model>(IEnumerable<Model> models)
        where Model : class => Operation.Try(async () =>
        {
            var entities = models
                .Select(model => _transformer.ToEntity(model, TransformCommand.Add))
                .ThrowIf(ContainsNull, new Exception("Invalid Graph List: one of the object-transformation returned null"));

            await _context.AddRangeAsync(entities);

            await _context.SaveChangesAsync();
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
            where Child : class => Operation.Try(async () =>
        {
            var refs = children
                .Select(child => _transformer.ToCollectionRef(
                    parent,
                    collectionProperty,
                    child,
                    TransformCommand.Add))
                .ToArray()
                .ThrowIf(ContainsNull, new Exception("Invalid Entity Ref found"));

            if (refs.Length == 0)
                throw new Exception("");


            //add?
            if (refs.All(IsManyToManyRef))
                await refs
                    .Select(@ref => @ref.Entity)
                    .ToArray()
                    .Pipe(_context.AddRangeAsync);

            //update?
            else if (refs.All(IsOneToManyRef))
                refs.Select(@ref => @ref.Entity)
                    .ToArray()
                    .Pipe(_context.UpdateRange);

            //twilight zone
            else throw new Exception("Invalid Entity Ref found");

            await _context.SaveChangesAsync();

            return refs
                .Select(@ref => @ref.Entity)
                .TransformAll<Child>(_transformer);
        });

        public Operation<Child[]> RemoveFromCollection<Parent, Child>(
            Parent parent,
            string collectionProperty,
            Child[] children)
            where Parent : class
            where Child : class => Operation.Try(async () =>
            {
                var refs = children
                    .Select(child => _transformer.ToCollectionRef(
                        parent,
                        collectionProperty,
                        child,
                        TransformCommand.Add))
                    .ToArray()
                    .ThrowIf(ContainsNull, new Exception("Invalid Entity Ref found"));

                if (refs.Length == 0)
                    throw new Exception("");


                //remove?
                if (refs.All(IsManyToManyRef))
                    refs.Select(@ref => @ref.Entity)
                        .ToArray()
                        .Pipe(_context.RemoveRange);

                //update?
                else if (refs.All(IsOneToManyRef))
                    refs.Select(@ref => @ref.Entity)
                        .ToArray()
                        .Pipe(_context.UpdateRange);

                //twilight zone
                else throw new Exception("Invalid Entity Ref found");

                await _context.SaveChangesAsync();

                return refs
                    .Select(@ref => @ref.Entity)
                    .TransformAll<Child>(_transformer);
            });


        private static bool IsManyToManyRef(EntityRef @ref) => @ref?.RefType == EntityRefType.ManyToMany;

        private static bool IsOneToManyRef(EntityRef @ref) => @ref?.RefType == EntityRefType.OneToMany;

        private static bool ContainsNull<T>(IEnumerable<T> list)  => list?.Any(obj => obj == null) == true;
    }
}
