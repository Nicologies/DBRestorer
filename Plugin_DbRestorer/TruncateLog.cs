using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DBRestorer.Plugin.Interface;
using Microsoft.Data.SqlClient;

namespace Plugin_DbRestorer
{
    [Export(typeof(IDbUtility))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TruncateLog :  IDbUtility 
    {
        public void Invoke(Window parentWnd, string sqlInstName, string dbName)
        {
            try
            {
                var connStrBuilder = new SqlConnectionStringBuilder
                {
                    InitialCatalog = dbName, DataSource = sqlInstName, IntegratedSecurity = true
                };
                using (var conn = new SqlConnection(connStrBuilder.ConnectionString))
                {
                    conn.Open();

                    var logName = GetLogLogicalName(conn);

                    using (var cmd = new SqlCommand($"DBCC SHRINKFILE (N'{logName}' , 0, TRUNCATEONLY)", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(parentWnd, $"Failed to truncate log {ex}");
            }
        }

        private static string GetLogLogicalName(SqlConnection conn)
        {
            using (var cmd = new SqlCommand("select name From dbo.sysfiles Where filename like '%.ldf'", conn))
            {
                return cmd.ExecuteScalar().ToString();
            }
        }

        public string PluginName => "Truncate Log";
    }
}
