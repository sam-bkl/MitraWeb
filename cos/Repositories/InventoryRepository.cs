using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using cos.ViewModels;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace cos.Repositories
{
    public class InventoryRepository
    {
        private readonly string connectionStringPgSql;
        private readonly string connectionStringPgSqlRO;

        public InventoryRepository(IConfiguration configuration)
        {
            connectionStringPgSql = configuration.GetValue<string>("ConnectionStrings:PgSql");
            connectionStringPgSqlRO = configuration.GetValue<string>("ConnectionStrings:PgSqlRO");
        }

        internal IDbConnection ConnectionPgSql
        {
            get
            {
                return new NpgsqlConnection(connectionStringPgSql);
            }
        }
        internal IDbConnection ConnectionPgSqlRO
        {
            get
            {
                return new NpgsqlConnection(connectionStringPgSqlRO);
            }
        }
        public async Task<dynamic> GetCircles()
        {
            StringBuilder query = new StringBuilder();
            query.Append("select circle_name, circle_code from circles");
            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();
                try
                {
                    var result = await dbConnection.QueryAsync(query.ToString());
                    return result.AsList();
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
        //Old slow implementation not suitable for production
        public async Task<dynamic> UploadSpareGSMNumbers(string filedata, string circle)
        {
            string[] msisdns = filedata.Split('\n');
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            var regex = @"^\d{10}$";
            StringBuilder query_check_existing = new StringBuilder();
            query_check_existing.Append("select 1 from gsm_choice where gsmno=@param_gsmno");
            StringBuilder query_check_sold = new StringBuilder();
            query_check_sold.Append("select 1 from gsm_choice_sold where gsmno=@param_gsmno");
            StringBuilder query_insert_number = new StringBuilder();
            query_insert_number.Append("insert into gsm_choice(gsmno, circle_code, status) values(@param_gsmno, @param_circle, '9')");
            using(IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();
                try
                {
                    foreach (string msisdn in msisdns)
                    {
                        var match = Regex.Match(msisdn, regex);
                        if (match.Success)
                        {
                            var result_existing = await dbConnection.QueryAsync<string>(query_check_existing.ToString(), new
                            {
                                param_gsmno=msisdn
                            });
                            var result_sold = await dbConnection.QueryAsync<string>(query_check_sold.ToString(), new
                            {
                                param_gsmno = msisdn
                            });
                            if (result_existing.FirstOrDefault() == "1" || result_sold.FirstOrDefault() == "1")
                            {
                                result.Add(new KeyValuePair<string, string>(msisdn, "Already existing gsmno"));
                            }
                            else
                            {
                                var result_upload = await dbConnection.ExecuteAsync(query_insert_number.ToString(), new
                                {
                                    param_gsmno = msisdn,
                                    param_circle = circle
                                });
                                result.Add(new KeyValuePair<string, string>(msisdn, "Uploaded successfully"));
                            }
                        }
                    }
                    return result;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        public async Task<dynamic> UploadSpareGSMNumbersFast(string filedata, string circle, string entry_by)
        {
            var result = new List<KeyValuePair<string, string>>();
            var regex = new Regex(@"^\d{10}$");

            using var conn =new NpgsqlConnection(connectionStringPgSql);
            
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();
            await new NpgsqlCommand("CREATE TEMP TABLE gsm_choice_stg(gsmno varchar(10), circle_code numeric(2), uploaded_by varchar(20)) ON COMMIT PRESERVE ROWS", conn, tx).ExecuteNonQueryAsync();
            await using (var writer = conn.BeginTextImport("COPY gsm_choice_stg (gsmno, circle_code, uploaded_by) FROM STDIN"))
            {
                foreach (var line in filedata.Split('\n'))
                {
                    var gsm = line.Trim();
                    if (regex.IsMatch(gsm))
                    {
                        await writer.WriteLineAsync($"{gsm}\t{circle}\t{entry_by}");
                    }
                }
            }

            var insertSql = @"
                INSERT INTO gsm_choice (gsmno, circle_code, status, uploaded_by)
                SELECT s.gsmno, s.circle_code, '9', s.uploaded_by
                FROM gsm_choice_stg s
                WHERE NOT EXISTS (
                    SELECT 1 FROM gsm_choice_sold cs
                    WHERE cs.gsmno = s.gsmno
                ) AND
                NOT EXISTS (
                    SELECT 1 FROM gsm_choice gs
                    WHERE gs.gsmno = s.gsmno
                )
                ON CONFLICT (gsmno) DO NOTHING;
            ";

            await new NpgsqlCommand(insertSql, conn, tx).ExecuteNonQueryAsync();

            var statusSql = @"
                SELECT
                    s.gsmno,
                    CASE
                        WHEN EXISTS (SELECT 1 FROM gsm_choice WHERE gsmno = s.gsmno)
                                THEN 'Uploaded successfully'
                        WHEN EXISTS (SELECT 1 FROM gsm_choice_sold WHERE gsmno = s.gsmno)
                                THEN 'Already sold gsmno'
                        ELSE 'Already existing gsmno'
                    END
                FROM gsm_choice_stg s
            ";

            await using var cmd = new NpgsqlCommand(statusSql, conn, tx);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Add(new KeyValuePair<string, string>(reader.GetString(0), reader.GetString(1)));
                }
            }
            await tx.CommitAsync();
            return result;
            
        }
        //Function to get the details of the CTOP UP Number
        public async Task<dynamic> GetCtopDetails(string ctopupno)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select * from ctop_master where ctopupno=@param_ctopupno");
            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();
                try
                {
                    var result = await dbConnection.QueryAsync(query.ToString(), new
                    {
                        param_ctopupno = ctopupno
                    });
                    return result.FirstOrDefault();
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
        //Function to set the aadhaar no to null for the given ctop up number
        public async Task<string> SetAadhaarToNull(string ctopupno)
        {
            StringBuilder query = new StringBuilder();
            query.Append("update ctop_master set aadhaar_no=null where ctopupno=@param_ctopupno");
            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();
                try
                {
                    var result = await dbConnection.ExecuteAsync(query.ToString(), new
                    {
                        param_ctopupno=ctopupno
                    });
                    return "Removed the aadhaar number for: " + ctopupno;
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
        public async Task<dynamic> GetPrepaidSummary(int circle_code)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select circle_code, location, count(*) total_sims, count(*) filter (where status=1) unallotted_sims, count(*) filter(where status=2) allotted_sims from simprepaid where circle_code=:param_circle_code group by circle_code, location order by circle_code, location");
            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();
                try
                {
                    var result = await dbConnection.QueryAsync(query.ToString(), new
                    {
                        param_circle_code=circle_code
                    });
                    return result.AsList();
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
        public async Task<dynamic> GetPrepaidDetails(int circle_code, string location, int status)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select circle_code, location, simno from simprepaid where circle_code=@param_circle_code and location=@param_location and status=@param_status ");
            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();
                try
                {
                    var result = await dbConnection.QueryAsync(query.ToString(), new
                    {
                        param_circle_code = circle_code,
                        param_location = location,
                        param_status = status
                    });
                    return result.AsList();
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
        public async Task<dynamic> GetCYMNSummary(int circle_code)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select circle_code, count(*) filter(where status=9) remaining_nums, count(*) filter(where status=99) reserved_nums from gsm_choice where circle_code=:param_circle_code group by circle_code");
            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();
                try
                {
                    var result = await dbConnection.QueryAsync(query.ToString(), new
                    {
                        param_circle_code = circle_code
                    });
                    return result.FirstOrDefault();
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
        public async Task<dynamic> GetCYMNDetails(int circle_code, int status)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select circle_code, gsmno from gsm_choice where circle_code=@param_circle_code and status=@param_status ");
            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();
                try
                {
                    var result = await dbConnection.QueryAsync(query.ToString(), new
                    {
                        param_circle_code = circle_code,
                        param_status = status
                    });
                    return result.AsList();
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
    }
}
