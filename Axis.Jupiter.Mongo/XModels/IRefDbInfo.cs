using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Jupiter.MongoDb.XModels
{
    public interface IRefDbInfo
    {
        /// <summary>
        /// The collection to which the referred entity belongs. Should not be null.
        /// </summary>
        string DbCollection { get; }

        /// <summary>
        /// The database within which the entity is located. May be null. Current database is assumed if it is null.
        /// </summary>
        string DbLabel { get; }
    }
}
