using System;
using System.Collections.Generic;
using System.Linq;
using Axis.Luna.Operation;
using Microsoft.EntityFrameworkCore;

namespace Axis.Jupiter.EFCore
{
    public class EFStoreCommand: IStoreCommand
    {
        private readonly DbContext _context;
        private readonly IModelTransformer _transformer;

        public string StoreName { get; }


        public EFStoreCommand(string storeName, DbContext context, IModelTransformer transformer)
        {
            StoreName = string.IsNullOrWhiteSpace(storeName)
                ? throw new Exception("Invalid Store Name specified")
                : storeName;

            _context = context ?? throw new Exception("Invalid Context specified: null");
            _transformer = transformer ?? throw new Exception("Invalid Model Transformer specified: null");
        }



        public Operation<long> Commit()
        => Operation.Try(async () =>
        {
            var result = await _context.SaveChangesAsync();
            return (long)result;
        });


        public Operation<Model> Add<Model>(Model d) 
        where Model : class => Operation.Try(async () =>
        {
            var entity = _transformer.ToEntity(d);
            entity = await _context.AddAsync(entity);

            return _transformer.ToModel<Model>(entity);
        });

        public Operation AddBatch<Model>(IEnumerable<Model> models)
        where Model : class => Operation.Try(async () =>
        {
            var entities = models.Select(_transformer.ToEntity);
            await _context.AddRangeAsync(entities);
        });


        public Operation<Model> Update<Model>(Model d)
        where Model : class => Operation.Try(() =>
        {
            var entity = _transformer.ToEntity(d);
            var entry = _context.Update(entity);

            return _transformer.ToModel<Model>(entry.Entity);
        });

        public Operation UpdateBatch<Model>(IEnumerable<Model> models)
        where Model : class => Operation.Try(() =>
        {
            var entities = models.Select(_transformer.ToEntity);
            _context.UpdateRange(entities);
        });


        public Operation<Model> Delete<Model>(Model d)
        where Model : class => Operation.Try(() =>
        {
            var entity = _transformer.ToEntity(d);
            var entry = _context.Remove(entity);

            return _transformer.ToModel<Model>(entry.Entity);
        });

        public Operation DeleteBatch<Model>(IEnumerable<Model> models)
        where Model : class => Operation.Try(() =>
        {
            var entities = models.Select(_transformer.ToEntity);
            _context.RemoveRange(entities);
        });
    }
}
