using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Jupiter.MongoDb.XModels
{
    public interface IRefPointer
    {
        IMongoEntity Entity { get; }
    }

    public interface IRefPointer<TKey>: IRefPointer
    {
        new IMongoEntity<TKey> Entity { get; }
    }
}
