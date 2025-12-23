using Oracle.ManagedDataAccess.Client;
using Dapper;
using cos.ViewModels;
using Npgsql;
using System.Data;
using Microsoft.Extensions.Caching.Memory;

namespace cos.Repositories
{
    public class SummaryRepository
    {
        private readonly string _connectionStringPgSql;
        private readonly string _connectionStringOracle;
        private readonly IMemoryCache _cache;

        public SummaryRepository(IConfiguration configuration,IMemoryCache cache)
        {
            _connectionStringPgSql = configuration.GetConnectionString("PgSql");
            _connectionStringOracle = configuration.GetConnectionString("OracleDb");
             _cache = cache;
        }

        private IDbConnection CreateConnection(){ return new NpgsqlConnection(_connectionStringPgSql);}
        
        private IDbConnection ConnectionOracle => new OracleConnection(_connectionStringOracle);

        /// <summary>
        /// KYC details grouped by Circle, POS and TYPE
        /// </summary>        
        public async Task<object> GetKycSummaryCircleWiseAsync()
        {
            const string sql = @"
                        SELECT
                            now() AS updated, 
                            -- If the join fails, these will show as 'Missing'
                            COALESCE(cc.zone_code, 'N/A') AS zone_code,                         
                            COALESCE(cc.circle_code, 'N/A') AS circle_code,                    
                            COALESCE(cc.short_code, 'N/A') AS short_code,    
                            agg.caf_type,
                            agg.total_request,
                            agg.completedkyc,
                            agg.rejected,
                            agg.seelater,
                            agg.not_verified
                        FROM (
                            SELECT
                                cb.circle_code, -- This is the raw code from your main data
                                cb.caf_type,
                                COUNT(*) AS total_request,
                                COUNT(*) FILTER (WHERE cb.verified_flag = 'Y') AS completedkyc,
                                COUNT(*) FILTER (WHERE cb.verified_flag = 'R') AS rejected,
                                COUNT(*) FILTER (WHERE cb.verified_flag = 'F') AS seelater,
                                COUNT(*) FILTER (WHERE cb.verified_flag NOT IN ('F','R','Y') OR cb.verified_flag IS NULL) AS not_verified
                            FROM cos_bcd cb
                            GROUP BY cb.circle_code, cb.caf_type
                        ) agg
                        -- Use LEFT JOIN to keep records even if circle_code is missing in the master table
                        LEFT JOIN cos_circles cc
                            ON cc.circle_code::numeric = agg.circle_code::numeric
                        ORDER BY
                            agg.circle_code,
                            agg.caf_type;
            ";


            using var conn = CreateConnection();
            return await conn.QueryAsync(sql);
        }
        public async Task<object?> GetKycSummaryCircleWiseAsyncCached()
        {
            return await _cache.GetOrCreateAsync(
                "kycCircleWise",               // cache key
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    var data = await GetKycSummaryCircleWiseAsync();

                    return data ?? new object(); //                    
                });
        }        
        
        /// <summary>
        /// activation details grouped by Circle, POS and TYPE
        /// </summary>        
        public async Task<IEnumerable<dynamic>> GetActivatedCircleWiseAsync()
        {
            const string sql = @"
                SELECT
                    CIRCLE_CODE,
                    ACT_TYPE,
                    COUNT(*) AS ACTIVATED
                FROM CAF_ADMIN.BCD
                WHERE ACTIVATION_STATUS = 'C'
                GROUP BY CIRCLE_CODE, ACT_TYPE
            ";

            using var conn = ConnectionOracle;

            try
            {
                conn.Open();

                return await conn.QueryAsync(
                    new CommandDefinition(sql)
                );
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Failed to fetch activated circle-wise data");
                throw;
            }
        }
        public async Task<object?> GetActivatedCircleWiseAsyncCached()
        {
            return await _cache.GetOrCreateAsync(
                "ActCircleWise",               // cache key
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    var data = await GetActivatedCircleWiseAsync();

                    return data ?? new object(); //                    
                });
        }                


        /// <summary>
        /// GetDayWiseActivationAsync
        /// </summary>        
        public async Task<object> GetDayWiseActivationAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT
            CASE
                WHEN GROUPING(TRUNC(INSERTED_TIME)) = 1
                THEN 'Total'
                ELSE TO_CHAR(TRUNC(INSERTED_TIME), 'DD-MON-YYYY')
            END AS ACTIVATION_DATE,
            CASE
                WHEN GROUPING(CIRCLE_CODE) = 1 THEN 'Total'
                ELSE DECODE(CIRCLE_CODE,
                    65, 'JAMMU AND KASHMIR',
                    61, 'HARYANA',
                    62, 'UPWEST',
                    55, 'HIMACHAL PRADESH',
                    56, 'PUNJAB',
                    59, 'RAJASTHAN',
                    64, 'UTTARANCHAL',
                    60, 'UPEAST',
                    2, 'DELHI',
                    71, 'ASSAM TELECOM CIRCLE',
                    70, 'WEST BENGAL TELECOM CIRCLE',
                    72, 'ODISHA TELECOM CIRCLE',
                    73, 'BIHAR TELECOM CIRCLE',
                    74, 'NORTH EAST-1 TELECOM CIRCLE',
                    75, 'NORTH EAST-2 TELECOM CIRCLE',
                    76, 'JHARKHAND TELECOM CIRCLE',
                    77, 'ANDHMAN TELECOM CIRCLE',
                    78, 'CALCUTTA TELECOM DISTRICT',
                    1, 'MAHARASHTRA',
                    3, 'CHHATTISGARH',
                    4, 'MADHYA PRADESH',
                    10, 'GUJARAT',
                    40, 'CHENNAI',
                    41, 'TELANGANA',
                    50, 'KERALA',
                    51, 'ANDHRA PRADESH',
                    53, 'KARNATAKA',
                    54, 'TAMIL NADU',
                    79, 'SIKKIM TELECOM CIRCLE',
                    99, 'SYSADMIN',
                    12, 'MUMBAI',
                    'UNKNOWN'
                )
            END AS CIRCLE_NAME,
            COUNT(*) AS TOTAL_C_ACTIVATIONS
        FROM CAF_ADMIN.BCD
        WHERE ACTIVATION_STATUS = 'C' 
            AND TRUNC(INSERTED_TIME) BETWEEN :startDate AND :endDate
            GROUP BY ROLLUP (
                TRUNC(INSERTED_TIME),
                CIRCLE_CODE
            )
            ";


            using var conn = ConnectionOracle;

            try
            {
                conn.Open();

                return await conn.QueryAsync(
                    new CommandDefinition(
                             commandText: sql,
                        parameters: new { startDate, endDate }));
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Failed to fetch activated circle-wise data");
                throw;
            }
        }


        /// <summary>
        /// GetDayWiseBillingStatusAsync
        /// </summary>        
        public async Task<object> GetCircleWiseBillingStatusAsync()
        {
            const string sql = @"
              SELECT
                CASE
                    WHEN GROUPING(ACTIVATION_STATUS) = 1
                    THEN 'TOTAL'
                    ELSE ACTIVATION_STATUS
                END AS ACTIVATION_STATUS,
                CASE
                    WHEN GROUPING(ACTIVATION_REMARKS) = 1
                    THEN 'TOTAL'
                    ELSE ACTIVATION_REMARKS
                END AS ACTIVATION_REMARKS,                
                CASE
                    WHEN GROUPING(CIRCLE_CODE) = 1 THEN 'TOTAL'
                    ELSE DECODE(CIRCLE_CODE,
                        65, 'JAMMU AND KASHMIR',
                        61, 'HARYANA',
                        62, 'UPWEST',
                        55, 'HIMACHAL PRADESH',
                        56, 'PUNJAB',
                        59, 'RAJASTHAN',
                        64, 'UTTARANCHAL',
                        60, 'UPEAST',
                        2, 'DELHI',
                        71, 'ASSAM TELECOM CIRCLE',
                        70, 'WEST BENGAL TELECOM CIRCLE',
                        72, 'ODISHA TELECOM CIRCLE',
                        73, 'BIHAR TELECOM CIRCLE',
                        74, 'NORTH EAST-1 TELECOM CIRCLE',
                        75, 'NORTH EAST-2 TELECOM CIRCLE',
                        76, 'JHARKHAND TELECOM CIRCLE',
                        77, 'ANDHMAN TELECOM CIRCLE',
                        78, 'CALCUTTA TELECOM DISTRICT',
                        1, 'MAHARASHTRA',
                        3, 'CHHATTISGARH',
                        4, 'MADHYA PRADESH',
                        10, 'GUJARAT',
                        40, 'CHENNAI',
                        41, 'TELANGANA',
                        50, 'KERALA',
                        51, 'ANDHRA PRADESH',
                        53, 'KARNATAKA',
                        54, 'TAMIL NADU',
                        79, 'SIKKIM TELECOM CIRCLE',
                        99, 'SYSADMIN',
                        12, 'MUMBAI',
                        'UNKNOWN'
                    )
                END AS CIRCLE_NAME,
                COUNT(*) AS TOTAL_C_ACTIVATIONS
            FROM CAF_ADMIN.BCD WHERE ACTIVATION_STATUS NOT IN ('C', 'TC','1')
            GROUP BY ROLLUP (
                    CIRCLE_CODE,
                    ACTIVATION_STATUS,
                    ACTIVATION_REMARKS
                    
                ) 
            ORDER BY CIRCLE_CODE,ACTIVATION_STATUS DESC , TOTAL_C_ACTIVATIONS DESC
            ";


            using var conn = ConnectionOracle;

            try
            {
                conn.Open();

                return await conn.QueryAsync(new CommandDefinition(commandText: sql));
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Failed to fetch activated circle-wise data");
                throw;
            }
        }

        /// <summary>
        /// GetBillingStatusAsync
        /// </summary>        
        public async Task<object> GetBillingStatusAsync()
        {
            const string sql = @"
            SELECT
                CASE
                    WHEN GROUPING(ACTIVATION_STATUS) = 1
                    THEN 'TOTAL'
                    ELSE ACTIVATION_STATUS
                END AS ACTIVATION_STATUS,
                CASE
                    WHEN GROUPING(ACTIVATION_REMARKS) = 1
                    THEN 'TOTAL'
                    ELSE ACTIVATION_REMARKS
                END AS ACTIVATION_REMARKS,                
                COUNT(*) AS TOTAL_C_ACTIVATIONS
            FROM CAF_ADMIN.BCD WHERE ACTIVATION_STATUS NOT IN ('C', 'TC','1')
            GROUP BY ROLLUP (
                    ACTIVATION_STATUS,
                    ACTIVATION_REMARKS
                    
                ) 
            ORDER BY ACTIVATION_STATUS DESC , TOTAL_C_ACTIVATIONS DESC
            ";


            using var conn = ConnectionOracle;

            try
            {
                conn.Open();

                return await conn.QueryAsync(new CommandDefinition(commandText: sql));
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Failed to fetch activated circle-wise data");
                throw;
            }
        }
          

        /// <summary>
        /// GetDayWiseActivationAsync
        /// </summary>        
        public async Task<object> GetDayWiseOnboardingAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"

                SELECT 
                    b.live_photo_time::DATE AS onboarding_date,
                    COALESCE(c.circle_name, 'TOTAL') AS circle_name,
                    SUM(b.cnt) AS counts
                FROM (
                    SELECT 
                        circle_code::TEXT AS circle_code,
                        live_photo_time::DATE AS live_photo_time,
                        COUNT(*) AS cnt
                    FROM cos_bcd 
                    WHERE live_photo_time::DATE BETWEEN :startDate AND :endDate
                    GROUP BY circle_code::TEXT, live_photo_time::DATE
                ) b
                INNER JOIN cos_circles c ON c.circle_code = b.circle_code
                GROUP BY ROLLUP(b.live_photo_time, c.circle_name)
                ORDER BY b.live_photo_time NULLS LAST, c.circle_name NULLS LAST;
            ";

            // postgres
            using var conn = CreateConnection();
            return await conn.QueryAsync(sql,new { startDate, endDate });
        }


        /// <summary>
        /// CircleWiseGSMInvetoryAsync
        /// </summary>        
        public async Task<object> CircleWiseGSMInvetoryAsync()
        {
            const string sql = @"
                    SELECT
                        CASE circle_code
                            WHEN 65 THEN 'JAMMU AND KASHMIR'
                            WHEN 61 THEN 'HARYANA'
                            WHEN 62 THEN 'UPWEST'
                            WHEN 55 THEN 'HIMACHAL PRADESH'
                            WHEN 56 THEN 'PUNJAB'
                            WHEN 59 THEN 'RAJASTHAN'
                            WHEN 64 THEN 'UTTARANCHAL'
                            WHEN 60 THEN 'UPEAST'
                            WHEN 2  THEN 'DELHI'
                            WHEN 71 THEN 'ASSAM TELECOM CIRCLE'
                            WHEN 70 THEN 'WEST BENGAL TELECOM CIRCLE'
                            WHEN 72 THEN 'ODISHA TELECOM CIRCLE'
                            WHEN 73 THEN 'BIHAR TELECOM CIRCLE'
                            WHEN 74 THEN 'NORTH EAST-1 TELECOM CIRCLE'
                            WHEN 75 THEN 'NORTH EAST-2 TELECOM CIRCLE'
                            WHEN 76 THEN 'JHARKHAND TELECOM CIRCLE'
                            WHEN 77 THEN 'ANDHMAN TELECOM CIRCLE'
                            WHEN 78 THEN 'CALCUTTA TELECOM DISTRICT'
                            WHEN 1  THEN 'MAHARASHTRA'
                            WHEN 3  THEN 'CHHATTISGARH'
                            WHEN 4  THEN 'MADHYA PRADESH'
                            WHEN 10 THEN 'GUJARAT'
                            WHEN 40 THEN 'CHENNAI'
                            WHEN 41 THEN 'TELANGANA'
                            WHEN 50 THEN 'KERALA'
                            WHEN 51 THEN 'ANDHRA PRADESH'
                            WHEN 53 THEN 'KARNATAKA'
                            WHEN 54 THEN 'TAMIL NADU'
                            WHEN 79 THEN 'SIKKIM TELECOM CIRCLE'
                            WHEN 12 THEN 'MUMBAI'
                            WHEN 99 THEN 'SYSADMIN'
                            ELSE 'UNKNOWN'
                        END AS circle_name,
                        COUNT(gsmno) AS gsm_count
                    FROM gsm_choice
                    WHERE status = 9
                    GROUP BY circle_code
                    ORDER BY gsm_count,circle_name;
            ";

            // postgres
            using var conn = CreateConnection();
            return await conn.QueryAsync(sql);
        }


    }
}