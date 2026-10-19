using System.Data.Entity.Migrations;

namespace Persistencia.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<AssistProContext>
    {
        public Configuration()
        {
            // Todo cambio al modelo debe pasar por Add-Migration para quedar versionado.
            AutomaticMigrationsEnabled = false;
        }
    }
}
