using System;
using System.Collections.Generic;
using System.Linq;
using Axis.Jupiter.Contracts;
using Axis.Luna.Operation;
using Microsoft.EntityFrameworkCore;

namespace Axis.Jupiter.EFCore
{
    public class EFStoreCommand: IStoreCommand
    {
        private readonly DbContext _context;
        private readonly ModelTransformer _transformer;

        public DbContext EFContext => _context;


        public EFStoreCommand(ModelTransformer transformer, DbContext context)
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
            var entities = models.Select(model => _transformer.ToEntity(model, TransformCommand.Add));
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

            return _transformer.ToModel<Model>(entry.Entity, TransformCommand.Delete);
        });

        public Operation DeleteBatch<Model>(IEnumerable<Model> models)
        where Model : class => Operation.Try(async () =>
        {
            var entities = models.Select(model => _transformer.ToEntity(model, TransformCommand.Delete));
            _context.RemoveRange(entities);

            await _context.SaveChangesAsync();
        });
    }
}
