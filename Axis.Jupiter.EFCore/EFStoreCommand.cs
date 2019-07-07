using System;
using System.Collections.Generic;
using System.Linq;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Models;
using Axis.Luna.Operation;
using Microsoft.EntityFrameworkCore;
using Axis.Luna.Extensions;
using System.Linq.Expressions;
using System.Reflection;
using Axis.Jupiter.Helpers;
using Axis.Jupiter.Services;

namespace Axis.Jupiter.EFCore
{
    public interface IEFStoreCommand: IStoreCommand
    {
        DbContext EFContext { get; }
    }

    public class EFStoreCommand: IEFStoreCommand
    {
        private readonly EntityMapper _mapper;

        public DbContext EFContext { get; }


        public EFStoreCommand(EntityMapper mapper, DbContext context)
        {
            EFContext = context ?? throw new Exception("Invalid Context specified: null");
            _mapper = mapper ?? throw new Exception("Invalid Entity Mapper specified: null");
        }

        #region Add
        public Operation<Model> Add<Model>(Model model) 
        where Model : class 
        => Operation.Try(async () =>
        {
            var entity = _mapper.ToEntity(model, MappingIntent.Add);
            var entry = await EFContext.AddAsync(entity);

            await EFContext.SaveChangesAsync();

            return _mapper.ToModel<Model>(entry.Entity, MappingIntent.Add);
        });

        public Operation AddBatch<Model>(IEnumerable<Model> models)
        where Model : class 
        => Operation.Try(async () =>
        {
            var entities = models
                .Select(model => _mapper.ToEntity(model, MappingIntent.Add))
                .ThrowIf(ContainsNull, new Exception("Invalid Graph List: one of the object-transformation returned null"));

            await EFContext.AddRangeAsync(entities);

            await EFContext.SaveChangesAsync();
        });
        #endregion

        #region Update
        public Operation<Model> Update<Model>(Model model)
        where Model : class => Operation.Try(async () =>
        {
            var entity = _mapper.ToEntity(model, MappingIntent.Update);
            var entry = EFContext.Update(entity);

            await EFContext.SaveChangesAsync();

            return _mapper.ToModel<Model>(entry.Entity, MappingIntent.Update);
        });

        public Operation UpdateBatch<Model>(IEnumerable<Model> models)
        where Model : class => Operation.Try(async () =>
        {
            var entities = models.Select(model => _mapper.ToEntity(model, MappingIntent.Update));
            EFContext.UpdateRange(entities);

            await EFContext.SaveChangesAsync();
        });
        #endregion

        #region Delete
        public Operation<Model> Delete<Model>(Model model)
        where Model : class 
        => Operation.Try(async () =>
        {
            var entity = _mapper.ToEntity(model, MappingIntent.Update);
            var entry = EFContext.Remove(entity);

            await EFContext.SaveChangesAsync();

            return _mapper.ToModel<Model>(entry.Entity, MappingIntent.Remove);
        });

        public Operation DeleteBatch<Model>(IEnumerable<Model> models)
        where Model : class 
        => Operation.Try(async () =>
        {
            var entities = models.Select(model => _mapper.ToEntity(model, MappingIntent.Remove));
            EFContext.RemoveRange(entities);

            await EFContext.SaveChangesAsync();
        });

        #endregion

        #region Collection 
        public Operation AddToCollection<Parent, Child>(
            Parent parent,
            Expression<Func<Parent, ICollection<Child>>> collectionPropertyExpression,
            params Child[] children)
            where Parent : class
            where Child : class
        => Operation.Try(async () =>
        {
            var infos = _mapper
                .ToCollectionRefInfo(
                    parent, 
                    MappingIntent.Add, 
                    collectionPropertyExpression, 
                    children)
                .ToArray();

            if (infos.Length == 0)
                return;

            await infos
                .GroupBy(info => info.Rank)
                .OrderByDescending(group => group.Key)
                .Select(async group =>
                {
                    await group
                        .GroupBy(info => info.Command)
                        .OrderBy(group2 => group2.Key)
                        .Select(async group2 =>
                        {
                            switch (group2.Key)
                            {
                                case CollectionRefCommand.Add:
                                    var arr = group2
                                        .Select(info => info.Entity)
                                        .ToArray();

                                    await EFContext.AddRangeAsync(arr);
                                    await EFContext.SaveChangesAsync();
                                    break;

                                case CollectionRefCommand.Update:
                                    arr = group2
                                        .Select(info => info.Entity)
                                        .ToArray();

                                    EFContext.UpdateRange(arr);
                                    await EFContext.SaveChangesAsync();
                                    break;

                                default: throw new Exception("Invalid Command: " + group.Key);
                            }
                        })
                        .Fold();
                })
                .Fold();

            var property = collectionPropertyExpression.Body
                .As<MemberExpression>().Member
                .As<PropertyInfo>();
            var modelCollection = parent.PropertyValue(property.Name) as ICollection<Child>;

            //add the children to the collection
            infos
                .Where(info => info.Result != RefInfoResult.None)
                .GroupBy(info => info.Result)
                .ForAll(group =>
                {
                    switch (group.Key)
                    {
                        case RefInfoResult.Entity:
                            modelCollection.AddRange(group.Select(info => _mapper.ToModel<Child>(
                                info.Entity, 
                                MappingIntent.Add)));
                            break;

                        case RefInfoResult.Model:
                            modelCollection.AddRange(group.Select(info => info.Model as Child));
                            break;
                    }
                });
        });

        public Operation RemoveFromCollection<Parent, Child>(
            Parent parent,
            Expression<Func<Parent, ICollection<Child>>> collectionPropertyExpression,
            params Child[] children)
            where Parent : class
            where Child : class
        => Operation.Try(async () =>
        {
            var infos = _mapper.ToCollectionRefInfo(
                parent,
                MappingIntent.Add,
                collectionPropertyExpression,
                children);

            await infos
                .GroupBy(info => info.Rank)
                .OrderByDescending(group => group.Key)
                .Select(async group =>
                {
                    await group
                        .GroupBy(info => info.Command)
                        .OrderBy(group2 => group2.Key)
                        .Select(async group2 =>
                        {
                            switch (group2.Key)
                            {
                                case CollectionRefCommand.Remove:
                                    var arr = group
                                        .Select(info => info.Entity)
                                        .ToArray();

                                    EFContext.RemoveRange(arr);
                                    await EFContext.SaveChangesAsync();
                                    break;

                                case CollectionRefCommand.Update:
                                    arr = group
                                        .Select(info => info.Entity)
                                        .ToArray();

                                    EFContext.UpdateRange(arr);
                                    await EFContext.SaveChangesAsync();
                                    break;

                                default: throw new Exception("Invalid Command: " + group.Key);
                            }
                        })
                        .Fold();
                })
                .Fold();

            var property = collectionPropertyExpression.Body
                .As<MemberExpression>().Member
                .As<PropertyInfo>();

            var modelCollection = parent.PropertyValue(property.Name) as ICollection<Child>;
            modelCollection.RemoveAll(children);
        });
        #endregion

        #region Misc
        private static bool ContainsNull<T>(IEnumerable<T> list) => list?.Any(t => t == null) == true;
        #endregion
    }
}
