using Axis.Luna.Extensions;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Axis.Jupiter.Europa
{
    public class BulkCopyOperation
    {
        internal Dictionary<SqlBulkCopy, DataTable> PayloadMap = new Dictionary<SqlBulkCopy, DataTable>();

        public void Execute() => PayloadMap.ForAll(_payload => _payload.Key.WriteToServer(_payload.Value));
    }
}
