using System;
using System.Data.Entity;

namespace Axis.Jupiter.Europa
{
    public class RootDbInitializer<Context>: IDatabaseInitializer<Context>
    where Context : EuropaContext
    {
        internal IDatabaseInitializer<Context> Initializer { get; private set; }
        private Action<Context> _aggregatedAction { get; set; }

        internal RootDbInitializer(IDatabaseInitializer<Context> initializer, Action<Context> aggregatedContextAction)
        {
            Initializer = initializer;
            _aggregatedAction = aggregatedContextAction;
        }

        public void InitializeDatabase(Context context)
        {
            //initialize the db
            Initializer?.InitializeDatabase(context);

            //seed the db
            _aggregatedAction?.Invoke(context);
            context.SaveChanges();
        }
    }
}
