
using System;
using System.Data.Entity;

namespace Axis.Jupiter.Europa
{
    internal class RootDbInitializer<Context>: IDatabaseInitializer<Context>
    where Context : EuropaContext
    {
        internal IDatabaseInitializer<Context> Initializer { get; private set; }
        private Action<Context> _Seeder { get; set; }

        internal RootDbInitializer(IDatabaseInitializer<Context> initializer, Action<Context> seeder)
        {
            Initializer = initializer;
            _Seeder = seeder;
        }

        public void InitializeDatabase(Context context)
        {
            //initialize the db
            Initializer?.InitializeDatabase(context);

            //seed the db
            _Seeder?.Invoke(context);
            context.SaveChanges();
        }
    }
}
