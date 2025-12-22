using Oracle.ManagedDataAccess.Client;
using Dapper;
using cos.ViewModels;
using Npgsql;
using System.Data;
using Microsoft.Extensions.Caching.Memory;

namespace cos.Repositories
{
    public class PosRepository
    {
        private readonly string _connectionStringPgSql;
        private readonly string _connectionStringOracle;
        private readonly IMemoryCache _cache;

        public PosRepository(IConfiguration configuration,IMemoryCache cache)
        {
            _connectionStringPgSql = configuration.GetConnectionString("PgSql");
            _connectionStringOracle = configuration.GetConnectionString("OracleDb");
             _cache = cache;
        }

        private IDbConnection CreateConnection(){ return new NpgsqlConnection(_connectionStringPgSql);}
        
        private IDbConnection ConnectionOracle => new OracleConnection(_connectionStringOracle);

        /// <summary>
        ///CtopupCircleMasterAsync
        /// </summary>        
        public async Task<object> CtopupCircleMasterAsync(string circle_code)
        {
            const string sql = @"
                SELECT 
                    cm.circle_name,  
                    cm.ssa_code,
                    cm.csccode,
                    cm.pos_unique_code,                    
                    cm.name,
                    cm.username,
                    cm.ctopupno,
                    cm.dealertype,
                    cm.attached_to,
                    -- cm.circle_code,
                    CASE 
                        WHEN cm.csccode = cm.attached_to THEN 'MASTER'
                        ELSE 'CHILD'
                    END AS user_role
                FROM 
                    ctop_master cm
                WHERE 
                    cm.end_date IS NOT null
                    and cm.circle_code = :circle_code
                 order by ssa_code,user_role desc,ctopupno,csccode
            ";

            // postgres
            using var conn = CreateConnection();
            return await conn.QueryAsync(sql,new {circle_code});
        }

        /// <summary>
        ///CtopupCircleSummaryAsync
        /// </summary>        
        public async Task<object> CtopupCircleSummaryAsync(string circle_code)
        {
            const string sql = @"

                SELECT
                    cm.ssa_code AS ba_code,
                    COALESCE(cm.dealertype, 'TOTAL') AS dealertype,
                    COUNT(*) AS total_count
                FROM
                    ctop_master cm
                WHERE
                    cm.end_date IS NOT NULL
                    AND cm.circle_code = :circle_code
                GROUP BY ROLLUP(cm.ssa_code, cm.dealertype)
                HAVING cm.ssa_code IS NOT NULL
                ORDER BY
                    cm.ssa_code,
                    CASE WHEN dealertype = 'TOTAL' THEN 1 ELSE 0 END,
                    cm.dealertype;
            ";

            // postgres
            using var conn = CreateConnection();
            return await conn.QueryAsync(sql,new {circle_code});
        }


      /// <summary>
        ///CtopupCircleMasterAsync
        /// </summary>        
        public async Task<object> CtopupBaMasterAsync(string circle_code,string ssa_code)
        {
            const string sql = @"
                SELECT 
                    cm.circle_name,  
                    cm.ssa_code,
                    cm.csccode,
                    cm.pos_unique_code,                    
                    cm.name,
                    cm.username,
                    cm.ctopupno,
                    cm.dealertype,
                    cm.attached_to,
                    -- cm.circle_code,
                    CASE 
                        WHEN cm.csccode = cm.attached_to THEN 'MASTER'
                        ELSE 'CHILD'
                    END AS user_role
                FROM 
                    ctop_master cm
                WHERE 
                    cm.end_date IS NOT null
                    and cm.ssa_code = :ssa_code
                    AND cm.circle_code = :circle_code
                 order by ssa_code,user_role desc,ctopupno,csccode
            ";

            // postgres
            using var conn = CreateConnection();
            return await conn.QueryAsync(sql,new {circle_code,ssa_code});
        }

        /// <summary>
        ///CtopupCircleSummaryAsync
        /// </summary>        
        public async Task<object> CtopupBaSummaryAsync(string circle_code,string ssa_code)
        {
            const string sql = @"

                SELECT
                    cm.ssa_code AS ba_code,
                    COALESCE(cm.dealertype, 'TOTAL') AS dealertype,
                    COUNT(*) AS total_count
                FROM
                    ctop_master cm
                WHERE
                    cm.end_date IS NOT NULL
                    AND cm.ssa_code = :ssa_code
                    AND cm.circle_code = :circle_code                    
                GROUP BY ROLLUP(cm.ssa_code, cm.dealertype)
                HAVING cm.ssa_code IS NOT NULL
                ORDER BY
                    cm.ssa_code,
                    CASE WHEN dealertype = 'TOTAL' THEN 1 ELSE 0 END,
                    cm.dealertype;
            ";

            // postgres
            using var conn = CreateConnection();
            return await conn.QueryAsync(sql,new {ssa_code,circle_code});
        }
        // public async Task<object?> GetKycSummaryCircleWiseAsyncCached()
        // {
        //     return await _cache.GetOrCreateAsync(
        //         "ctopup",               // cache key
        //         async entry =>
        //         {
        //             entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        //             var data = await GetKycSummaryCircleWiseAsync();

        //             return data ?? new object(); //                    
        //         });
        // }        
        
        // /// <summary>
        // /// activation details grouped by Circle, POS and TYPE
        // /// </summary>        
        // public async Task<IEnumerable<dynamic>> GetActivatedCircleWiseAsync()
        // {
        //     const string sql = @"
        //         SELECT
        //             CIRCLE_CODE,
        //             ACT_TYPE,
        //             COUNT(*) AS ACTIVATED
        //         FROM CAF_ADMIN.BCD
        //         WHERE ACTIVATION_STATUS = 'C'
        //         GROUP BY CIRCLE_CODE, ACT_TYPE
        //     ";

        //     using var conn = ConnectionOracle;

        //     try
        //     {
        //         conn.Open();

        //         return await conn.QueryAsync(
        //             new CommandDefinition(sql)
        //         );
        //     }
        //     catch (Exception ex)
        //     {
        //         // _logger.LogError(ex, "Failed to fetch activated circle-wise data");
        //         throw;
        //     }
        // }



    }
}