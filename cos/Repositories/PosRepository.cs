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
                    cm.aadhaar_no,
                    cm.pos_unique_code,                    
                    cm.name,
                    cm.username,
                    cm.ctopupno,
                    cm.dealertype,
                    cm.attached_to,
                    CASE 
                        WHEN cm.csccode = cm.attached_to THEN 'MASTER'
                        ELSE 'CHILD'
                    END AS user_role,
                    CASE 
                        WHEN cm.end_date IS NULL THEN 'ACTIVE'
                        ELSE 'DEACTIVE'
                    END AS state                    
                FROM 
                    ctop_master cm
                WHERE 
                    cm.circle_code = :circle_code
                ORDER BY 
                    ssa_code,
                    user_role DESC,
                    ctopupno,
                    csccode;
            ";

            // postgres
            using var conn = CreateConnection();
            return await conn.QueryAsync(sql,new {circle_code});
        }

        public async Task<object?> CtopupCircleMasterAsyncCached(string circle_code)
        {

                var cacheKey = $"ctopup_{circle_code}";

                if (_cache.TryGetValue(cacheKey, out object? cachedData))
                {
                    return cachedData;
                }

                var data = await CtopupCircleMasterAsync(circle_code);

                if (data != null)
                {
                    _cache.Set(
                        cacheKey,
                        data,
                        TimeSpan.FromMinutes(10)
                    );
                }

                return data;            
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
                    sum(
                    CASE 
                        WHEN cm.end_date is null THEN 1
                        ELSE 0
                    END  )AS active, 
                    sum(
                    CASE 
                        WHEN cm.end_date is null THEN 0
                        ELSE 1
                    END  )AS inactive,                      
                    COUNT(*) AS total_count
                FROM
                    ctop_master cm
                WHERE
                    -- cm.end_date IS NOT NULL
                    cm.circle_code = :circle_code
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

        public async Task<object?> CtopupCircleSummaryAsyncCached(string circle_code)
        {

                var cacheKey = $"ctopup_c_sum_{circle_code}";

                if (_cache.TryGetValue(cacheKey, out object? cachedData))
                {
                    return cachedData;
                }

                var data = await CtopupCircleSummaryAsync(circle_code);

                if (data != null)
                {
                    _cache.Set(
                        cacheKey,
                        data,
                        TimeSpan.FromMinutes(10)
                    );
                }

                return data;
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
                    cm.aadhaar_no,
                    cm.pos_unique_code,                    
                    cm.name,
                    cm.username,
                    cm.ctopupno,
                    cm.dealertype,
                    cm.attached_to,
                    CASE 
                        WHEN cm.csccode = cm.attached_to THEN 'MASTER'
                        ELSE 'CHILD'
                    END AS user_role,
                    CASE 
                        WHEN cm.end_date IS NULL THEN 'ACTIVE'
                        ELSE 'DEACTIVE'
                    END AS state                    
                FROM 
                    ctop_master cm
                WHERE 
                    cm.circle_code = :circle_code
                    and cm.ssa_code = :ssa_code
                ORDER BY 
                    ssa_code,
                    user_role DESC,
                    ctopupno,
                    csccode;
            ";

            // postgres
            using var conn = CreateConnection();
            return await conn.QueryAsync(sql,new {circle_code,ssa_code});
        }

        public async Task<object?> CtopupBaMasterAsyncCached(string circle_code,string ssa_code)
        {


                var cacheKey = $"ctopup_ba{ssa_code}";

                if (_cache.TryGetValue(cacheKey, out object? cachedData))
                {
                    return cachedData;
                }

                var data = await CtopupBaMasterAsync(circle_code,ssa_code);

                if (data != null)
                {
                    _cache.Set(
                        cacheKey,
                        data,
                        TimeSpan.FromMinutes(10)
                    );
                }

                return data; 

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
                    sum(
                    CASE 
                        WHEN cm.end_date is null THEN 1
                        ELSE 0
                    END  )AS active, 
                    sum(
                    CASE 
                        WHEN cm.end_date is null THEN 0
                        ELSE 1
                    END  )AS inactive,                      
                    COUNT(*) AS total_count
                FROM
                    ctop_master cm
                WHERE
                              
                    cm.circle_code = :circle_code
                    AND cm.ssa_code = :ssa_code                    
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
        
        public async Task<object?> CtopupBaSummaryAsyncCached(string circle_code,string ssa_code)
        {


               var cacheKey = $"ctopup_ba_sum{ssa_code}";

                if (_cache.TryGetValue(cacheKey, out object? cachedData))
                {
                    return cachedData;
                }

                var data = await CtopupBaSummaryAsync(circle_code,ssa_code);

                if (data != null)
                {
                    _cache.Set(
                        cacheKey,
                        data,
                        TimeSpan.FromMinutes(10)
                    );
                }

                return data; 
        }
        
      



    }
}