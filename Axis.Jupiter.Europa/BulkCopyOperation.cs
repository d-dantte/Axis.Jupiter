using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Axis.Jupiter.Europa
{
    public class BulkCopyOperation: IDisposable
    {
        internal Dictionary<SqlBulkCopyContext, DataTable> PayloadMap = new Dictionary<SqlBulkCopyContext, DataTable>();

        public Action<SqlBulkCopyContext> PreExecute { get; internal set; }
        public Action<SqlBulkCopyContext> PostExecute { get; internal set; }

        public void Execute()
        {
            PayloadMap.ForAll(_payload =>
            {
                if (_payload.Key.Connection.State == ConnectionState.Closed)
                    _payload.Key.Connection.Open();

                PreExecute?.Invoke(_payload.Key);

                _payload.Key.Context.WriteToServer(_payload.Value);

                PostExecute?.Invoke(_payload.Key);
            });
        }

        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            else if (disposing)
                PayloadMap.Keys.ForAll(_bcp => _bcp.Context.Close());

            disposed = true;
        }
    }

    public class SqlBulkCopyContext
    {
        public SqlBulkCopy Context { get; private set; }
        public SqlConnection Connection { get; private set; }

        public SqlBulkCopyContext(SqlConnection connection, SqlBulkCopyOptions options = SqlBulkCopyOptions.Default)
        {
            Connection = connection.ThrowIfNull("invalid connection");
            Context = new SqlBulkCopy(connection, options, null);
        }
    }
}