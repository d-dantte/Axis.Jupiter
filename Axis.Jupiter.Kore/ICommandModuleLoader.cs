namespace Axis.Jupiter.Kore
{
    public interface ICommandModuleLoader
    {
        void LoadCommands(PersistenceProvider.Registrar operationRegistrar);
    }
}
