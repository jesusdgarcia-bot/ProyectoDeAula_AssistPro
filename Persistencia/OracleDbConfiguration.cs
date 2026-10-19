using System.Data.Entity;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.EntityFramework;

namespace Persistencia
{
    /// <summary>
    /// Registra el proveedor de Oracle para EF6 por codigo, de modo que el App.config
    /// solo necesite la cadena de conexion.
    /// </summary>
    public class OracleDbConfiguration : DbConfiguration
    {
        public OracleDbConfiguration()
        {
            SetProviderServices("Oracle.ManagedDataAccess.Client", EFOracleProviderServices.Instance);
            SetProviderFactory("Oracle.ManagedDataAccess.Client", new OracleClientFactory());
            SetDefaultConnectionFactory(new OracleConnectionFactory());
        }
    }
}
