using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace ScraperModeloCoche.Services
{
    /// <summary>
    /// Clase que establece la conexión a la BBDD y permite realizar consultas obteniendo un DataTable
    /// </summary>
    public class ServicioDB
    {
        private static ServicioDB _instance;
        private static string _cnStr;

        private ServicioDB()
        {
            _cnStr = Entorno.CadenaConexion;
        }

        public static ServicioDB Instancia
        {
            get
            {
                if (_instance == null || _cnStr != Entorno.CadenaConexion)
                {
                    _instance = new ServicioDB();
                }
                return _instance;
            }
        }

        public SqlConnection GetSqlConnection()
        {
            return new SqlConnection(_cnStr);
        }

        public async Task<bool> BulkInsertAsync(DataTable dataTable)
        {
            using (var copy = new SqlBulkCopy(_cnStr, SqlBulkCopyOptions.Default))
            {
                copy.DestinationTableName = dataTable.TableName;
                try
                {
                    await copy.WriteToServerAsync(dataTable);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Data);
                    return false;
                }
                catch (SqlException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Data);
                    foreach (SqlError err in ex.Errors)
                    {
                        Console.WriteLine($"{err.Message}_{err.Procedure}_{err.LineNumber}");
                    }
                    return false;
                }

                return true;
            }
        }

        public async Task<bool> BulkInsertAsync(DataTable dataTable, string targetTableName = null, SqlBulkCopyOptions options = SqlBulkCopyOptions.Default)
        {
            using (var copy = new SqlBulkCopy(_cnStr, options))
            {
                copy.DestinationTableName = string.IsNullOrEmpty(targetTableName) ? dataTable.TableName : targetTableName;
                try
                {
                    await copy.WriteToServerAsync(dataTable);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Data);
                    return false;
                }
                catch (SqlException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Data);
                    foreach (SqlError err in ex.Errors)
                    {
                        Console.WriteLine($"{err.Message}_{err.Procedure}_{err.LineNumber}");
                    }
                    return false;
                }

                return true;
            }
        }

        public async Task<bool> BulkInsertAsync(DataTable dataTable, List<string> columnMapping, string targetTableName = null,
           SqlBulkCopyOptions options = SqlBulkCopyOptions.Default, SqlConnection cn =null, SqlTransaction tr = null)
        {

            using (var copy = new SqlBulkCopy(cn, options, tr))
            {
                copy.DestinationTableName = string.IsNullOrEmpty(targetTableName) ? dataTable.TableName : targetTableName;
                copy.BatchSize = dataTable.Rows.Count;

                if (columnMapping != null)
                {
                    foreach (var column in columnMapping)
                    {
                        var split = column.Split(new[] { ':' });
                        copy.ColumnMappings.Add(split.First(), split.Last());
                    }
                }

                try
                {
                    await copy.WriteToServerAsync(dataTable);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Data);
                    return false;
                }
                catch (SqlException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Data);
                    foreach (SqlError err in ex.Errors)
                    {
                        Console.WriteLine($"{err.Message}_{err.Procedure}_{err.LineNumber}");
                    }
                    return false;
                }

                return true;
            }
        }       

       
        
        public async Task<bool> BulkInsertAsync(DataTable dataTable, string targetTableName, SqlTransaction transaction, SqlBulkCopyOptions options = SqlBulkCopyOptions.Default, List<string> columnMapping = null)
        {
            if (transaction == null || transaction.Connection == null)
            {
                throw new ArgumentException("Transaction cannot be null or have a null connection.");
            }

            using (var copy = new SqlBulkCopy(transaction.Connection, options, transaction))
            {
                copy.DestinationTableName = targetTableName;
                copy.BatchSize = dataTable.Rows.Count;

                if (columnMapping != null)
                {
                    foreach (var column in columnMapping)
                    {
                        var split = column.Split(new[] { ':' });
                        copy.ColumnMappings.Add(split.First(), split.Last());
                    }
                }

                try
                {
                    await copy.WriteToServerAsync(dataTable);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Data);
                    return false;
                }
                catch (SqlException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Data);
                    foreach (SqlError err in ex.Errors)
                    {
                        Console.WriteLine($"{err.Message}_{err.Procedure}_{err.LineNumber}");
                    }
                    return false;
                }

                return true;
            }
        }


        public async Task<bool> ExecuteStoredProcedure(string spName, DynamicParameters parameters = null)
        {
            try
            {
                using (var cn = new SqlConnection(_cnStr))
                {
                    if (cn.State == ConnectionState.Closed) await cn.OpenAsync();
                    var res = await cn.ExecuteAsync(spName, parameters, commandType: CommandType.StoredProcedure);
                    return res > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing stored procedure '{spName}': {ex.Message}");
                return false;
            }
        }

        public async Task<List<T>> ExecuteStoredProcedure<T>(string spName, object parameters = null)
        {
            try
            {
                using (var cn = new SqlConnection(_cnStr))
                {
                    if (cn.State == ConnectionState.Closed) await cn.OpenAsync();
                    var res = await cn.QueryAsync<T>(spName, parameters, commandType: CommandType.StoredProcedure);
                    return res.Any() ? res.ToList() : new List<T>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing stored procedure '{spName}' with return type '{typeof(T)}': {ex.Message}");
                return new List<T>();
            }
        }

        public async Task<bool> ExecuteCommand(string query, object parameters = null)
        {
            try
            {
                using (var cn = new SqlConnection(_cnStr))
                {
                    if (cn.State == ConnectionState.Closed) await cn.OpenAsync();
                    int rows = await cn.ExecuteScalarAsync<int>(query, parameters);
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command '{query}': {ex.Message}");
                return false;
            }
        }

        public async Task<T> ExecuteCommandScalarRetrieve<T>(string query, object parameters = null)
        {
            try
            {
                using (var cn = new SqlConnection(_cnStr))
                {
                    if (cn.State == ConnectionState.Closed) await cn.OpenAsync();
                    var res = await cn.ExecuteScalarAsync(query, parameters);
                    if (res != null && res != DBNull.Value)
                    {
                        return (T)Convert.ChangeType(res, typeof(T));
                    }
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                // Preparar una cadena con la consulta y los parámetros para depuración
                string queryWithParameters = query;
                if (parameters != null)
                {
                    var parameterValues = parameters as DynamicParameters;
                    if (parameterValues != null)
                    {
                        var paramDict = parameterValues.ParameterNames.ToDictionary(paramName => paramName, paramName => parameterValues.Get<dynamic>(paramName));
                        foreach (var param in paramDict)
                        {
                            queryWithParameters = queryWithParameters.Replace($"@{param.Key}", param.Value.ToString());
                        }
                    }
                }
                Console.WriteLine($"Error executing command: '{queryWithParameters}': {ex.Message}");
                return default(T);
            }
        }


        public async Task<bool> ExecuteCommandAsync(string query, object parameters = null)
        {
            try
            {
                using (var cn = new SqlConnection(_cnStr))
                {
                    if (cn.State == ConnectionState.Closed) await cn.OpenAsync();
                    int rows = await cn.ExecuteAsync(query, parameters);
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                var parameterDetails = parameters == null ? "No parameters" : string.Join(", ", parameters.GetType().GetProperties().Select(p => $"{p.Name}={p.GetValue(parameters)}"));
                Console.WriteLine($"Error al ejecutar el comando '{query}' con parámetros: {parameterDetails}. Error: {ex.Message}");
                return false;
            }
        }


        public async Task<T> GetSingleDataAsync<T>(string query, object param = null)
        {
            try
            {
                using (var cn = new SqlConnection(_cnStr))
                {
                    if (cn.State == ConnectionState.Closed) await cn.OpenAsync();
                    return await cn.QuerySingleOrDefaultAsync<T>(query, param);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting single data async for query '{query}' with return type '{typeof(T)}': {ex.Message}");
                return default(T);
            }
        }

        public T GetSingleData<T>(string query, object param = null)
        {
            try
            {
                using (var cn = new SqlConnection(_cnStr))
                {
                    if (cn.State == ConnectionState.Closed) cn.Open();
                    return cn.QuerySingleOrDefault<T>(query, param);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting single data for query '{query}' with return type '{typeof(T)}': {ex.Message}");
                return default(T);
            }
        }


        /// <summary>
        /// Método de consulta a la BBDD
        /// </summary>
        /// <param name="query">SQL Query</param>
        /// <param name="tableName">Nombre de la tabla para identificar el DataTable</param>
        /// <returns></returns>
        public async Task<DataTable> GetData(string query, string tableName = "db_table", int cmdTimeoutSegundos = 30)
        {
            try
            {
                using (var cn = new SqlConnection(_cnStr))
                {
                    if (cn.State == ConnectionState.Closed) await cn.OpenAsync();
                    SqlCommand cmd = new SqlCommand(query, cn)
                    {
                        CommandTimeout = cmdTimeoutSegundos
                    };
                    var adGrid = new SqlDataAdapter(cmd);

                    var dtGrid = new DataTable(tableName);
                    adGrid.Fill(dtGrid);
                    return dtGrid;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<DataTable> GetDataWithParameters(string query, string tableName = "db_table", DynamicParameters parametros = null, int cmdTimeoutSegundos = 30)
        {
            try
            {
                using (var cn = new SqlConnection(_cnStr))
                {
                    if (cn.State == ConnectionState.Closed) await cn.OpenAsync();

                    using (var cmd = new SqlCommand(query, cn))
                    {
                        cmd.CommandTimeout = cmdTimeoutSegundos;

                        if (parametros != null)
                        {
                            foreach (var paramName in parametros.ParameterNames)
                            {
                                cmd.Parameters.AddWithValue(paramName, parametros.Get<object>(paramName));
                            }
                        }

                        var adGrid = new SqlDataAdapter(cmd);
                        var dtGrid = new DataTable(tableName);
                        adGrid.Fill(dtGrid);
                        return dtGrid;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }


        public async Task<List<T>> GetData<T>(string query, object parameters = null, CommandType? cmdType = null)
        {
            using (var cn = new SqlConnection(_cnStr))
            {
                if (cn.State == ConnectionState.Closed) await cn.OpenAsync();
                var res = await cn.QueryAsync<T>(query, parameters, commandType: cmdType);
                return res.AsList();
            }
        }
    }
}
