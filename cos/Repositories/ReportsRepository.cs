using Dapper;
using cos.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Linq;
using System.Text;

namespace cos.Repositories
{
    public class ReportsRepository
    {
        private readonly string connectionStringPgSql;

        public ReportsRepository(IConfiguration configuration)
        {
            connectionStringPgSql = configuration.GetValue<string>("ConnectionStrings:PgSql");
        }

        internal IDbConnection ConnectionPgSql
        {
            get
            {
                return new NpgsqlConnection(connectionStringPgSql);
            }
        }

      

        public async Task<List<PostpaidSummaryVM>> GetpostSummary(string circle, string role, string ssa)
        {

            List<PostpaidSummaryVM> postSummary = new List<PostpaidSummaryVM>();

            StringBuilder query = new StringBuilder();
           
            
                query.Append("select circle_code ,location, count(*) loccount from simpostpaid group by location,circle_code ");
                
         

            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();

                try
                {
                    var result = await dbConnection.QueryAsync<PostpaidSummaryVM>(query.ToString(), new { circle = circle, oa = ssa });

                    postSummary = result.AsList();
                    return postSummary;
                }
                catch (Exception)
                {
                    return postSummary;
                }


            }

        }

        public async Task<List<PostpaiddetailsVM>> Getpostdetails(int circle, string location, string role, string ssa)
        {
            List<PostpaiddetailsVM> postdetails = new List<PostpaiddetailsVM>();

            StringBuilder query = new StringBuilder();
            query.Append("SELECT circle_code, location, simno, imsi ");
            query.Append("FROM simpostpaid ");
            query.Append("WHERE circle_code = @circle AND location = @location");

            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();

                try
                {
                    var result = await dbConnection.QueryAsync<PostpaiddetailsVM>(
                        query.ToString(),
                        new { circle = circle, location = location }
                    );

                    postdetails = result.AsList();
                    return postdetails;
                }
                catch (Exception)
                {
                    return postdetails;
                }
            }
        }


        public async Task<bool> UpdateSACustomerIdAsync(string gsmnumber)
        {
            string query = @"
        UPDATE cos_bcd 
        SET verified_flag = 'Y' 
        WHERE gsmnumber = @gsmnumber;
    ";

            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();
                try
                {
                    int rowsAffected = await dbConnection.ExecuteAsync(
                        query,
                        new { gsmnumber } // parameter name must match SQL
                    );

                    return rowsAffected > 0;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }


        //    public async Task<UsageVM> GetUsage(string customer)
        //    {

        //        UsageVM usg = new UsageVM();

        //        StringBuilder query = new StringBuilder();
        //        int sMonth = DateTime.Now.Month;
        //        int sYear = DateTime.Now.Year;

        //        //query.Append("select sum(p.usrdata) total_data,sum(p.usrsms) total_sms from(select point_origin, account_no, sum(CASE WHEN type_id_usg in ('23731', '23732')  THEN primary_units ");
        //        //query.Append("ELSE 0  END) usrdata,sum(CASE WHEN type_id_usg in ('23126', '23128')  THEN primary_units ");
        //        //query.Append("ELSE 0  END) usrsms from cdr where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");            
        //        //query.Append("  and date_part('month',trans_dt)= @param_month and date_part('year',trans_dt)= @param_year group by point_origin,account_no)p ");

        //        query.Append("select sum(p.usrdata) total_data from(select point_origin, account_no,sum(primary_units) usrdata");
        //        query.Append(" from cdr_test where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");
        //        query.Append("  and date_part('month',trans_dt)= @param_month and date_part('year',trans_dt)= @param_year group by point_origin,account_no)p ");



        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<UsageVM>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_month = sMonth,
        //                    param_year = sYear
        //                    // param_circle = "KL"
        //                });

        //                return result.FirstOrDefault();

        //            }
        //            catch (Exception)
        //            {
        //                return usg;
        //            }


        //        }

        //    }

        //    public async Task<NotifyVM> GetNotifications(string customer)
        //    {

        //        NotifyVM msgs = new NotifyVM();

        //        StringBuilder query = new StringBuilder();
        //        query.Append("select count(*) total_msg from notifications");
        //        query.Append(" where circle_code = @param_circle and custname =  @param_customer");



        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<NotifyVM>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_circle = "KL"
        //                });

        //                return result.FirstOrDefault();

        //            }
        //            catch (Exception)
        //            {
        //                return msgs;
        //            }


        //        }

        //    }



        //    public async Task<List<NotifyMsgsVM>> GetNotificationsMsgs(string customer)
        //    {
        //        List<NotifyMsgsVM> msgsVM = new List<NotifyMsgsVM>();

        //        StringBuilder query = new StringBuilder();
        //        query.Append("select message,date from notifications");
        //        query.Append(" where circle_code = @param_circle and custname =  @param_customer");



        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<NotifyMsgsVM>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_circle = "KL"
        //                });

        //                msgsVM = result.AsList();
        //                return msgsVM;

        //            }
        //            catch (Exception)
        //            {
        //                return msgsVM;
        //            }


        //        }

        //    }

        //    public async Task<int> ChkData(string customer, string circle,string account_no)
        //    {



        //        StringBuilder query = new StringBuilder();
        //        query.Append("select count(*) chkcnt");
        //        query.Append(" from accountmaster where circle_code = @param_circle and custname=  @param_customer and account_no= @param_accountno");



        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<int>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_accountno = account_no,
        //                    param_circle = circle
        //                });

        //                return result.FirstOrDefault();

        //            }
        //            catch (Exception err)
        //            {
        //                 return -1;
        //            }


        //        }

        //    }

        //    public async Task<string> AddData(string customer, string circle,string account_no)
        //    {
        //        var param = new DynamicParameters();
        //        param.Add(name: "param_accountno", value: Int32.Parse(account_no), direction: ParameterDirection.Input);
        //        param.Add(name: "@param_customer", value: customer, direction: ParameterDirection.Input);
        //        param.Add(name: "@param_circle", value: circle, direction: ParameterDirection.Input);


        //        StringBuilder query = new StringBuilder();
        //        query.Append("Insert into accountmaster(account_no,custname,circle_code,entrydate)");
        //        query.Append(" values(@param_accountno,@param_customer,@param_circle,now())");



        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();
        //            var transaction = dbConnection.BeginTransaction();

        //            try
        //            {
        //                var result = await dbConnection.ExecuteAsync(query.ToString(), param);
        //                transaction.Commit();
        //                return "success";

        //            }
        //            catch (Exception err)
        //            {
        //                transaction.Rollback();
        //                return err.Message;
        //            }


        //        }

        //    }

        //    public async Task<List<TopSimsVM>> GetTopSims(string customer)
        //    {

        //        List<TopSimsVM> topSims = new List<TopSimsVM>();
        //        int sMonth = DateTime.Now.Month;
        //        int sYear = DateTime.Now.Year;


        //        StringBuilder query = new StringBuilder();
        //        // query.Append("select point_origin msisdn,sum(CASE WHEN type_id_usg in ('23731','23732')  THEN primary_units ELSE 0 END) usrdata ");
        //        // query.Append("from cdrTest where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");
        //        // query.Append(" and date_part('month',trans_dt)= @param_month and date_part('year',trans_dt)= @param_year group by point_origin,account_no order by usrdata desc limit 10;");

        //         query.Append("select point_origin msisdn,sum(primary_units) usrdata ");
        //         query.Append("from cdr_test where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");
        //         query.Append(" and date_part('month',trans_dt)= @param_month and date_part('year',trans_dt)= @param_year group by point_origin,account_no order by usrdata desc limit 10;");

        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<TopSimsVM>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_month = sMonth,
        //                    param_year = sYear
        //                    // param_circle = "KL"
        //                });

        //                topSims = result.AsList();

        //                return topSims;
        //            }
        //            catch (Exception)
        //            {
        //                return topSims;
        //            }


        //        }

        //    }

        //    public async Task<List<BarUsgVM>> GetBarUsg(string customer)
        //    {

        //        List<BarUsgVM> barUsgs = new List<BarUsgVM>();

        //        StringBuilder query = new StringBuilder();
        //        //query.Append("select sum(p.usrdata) custdata,sum(p.usrsms) custsms,to_char(to_timestamp (p.tp::text, 'MM'), 'TMMon') || ");
        //        //query.Append("to_char(to_timestamp(p.ty::text, 'YYYY'), 'YY') labl from (select date_part('month',trans_dt) tp,");
        //        //query.Append("date_part('year',trans_dt) ty,account_no,sum(CASE WHEN type_id_usg in ('23731','23732')  THEN primary_units ELSE 0 END) usrdata,");
        //        //query.Append("sum(case WHEN type_id_usg in ('23126','23128')  THEN primary_units ELSE 0 END) usrsms ");
        //        //query.Append("from cdr where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");
        //        //query.Append(" group by account_no,date_part('month',trans_dt),date_part('year',trans_dt))p group by p.tp,p.ty  order by p.tp;");


        //        query.Append("select sum(p.usrdata) custdata,to_char(to_timestamp (p.tp::text, 'MM'), 'TMMon') || ");
        //        query.Append("to_char(to_timestamp(p.ty::text, 'YYYY'), 'YY') labl from (select date_part('month',trans_dt) tp,");
        //        query.Append("date_part('year',trans_dt) ty,account_no,sum(primary_units) usrdata ");           
        //        query.Append("from cdr_test where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");
        //        query.Append(" group by account_no,date_part('month',trans_dt),date_part('year',trans_dt))p group by p.tp,p.ty  order by p.tp;");



        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<BarUsgVM>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    // param_circle = "KL"
        //                });

        //                barUsgs = result.AsList();

        //                return barUsgs;
        //            }
        //            catch (Exception)
        //            {
        //                return barUsgs;
        //            }


        //        }

        //    }

        //    public async Task<List<LineUsgVM>> GetLineUsg(string customer, int filterYear, int filterMonth)
        //    {
        //        int year = filterYear;
        //        int month = filterMonth;
        //        List<LineUsgVM> lineUsgs = new List<LineUsgVM>();

        //        StringBuilder query = new StringBuilder();
        //        //query.Append("select sum(p.usrdata) custdata,sum(p.usrsms) custsms,td labl from (select date_part('day',trans_dt) td,date_part('month',trans_dt) tp,");
        //        //query.Append("date_part('year',trans_dt) ty,account_no,sum(CASE WHEN type_id_usg in ('23731','23732')  THEN primary_units ELSE 0 END) usrdata,");
        //        //query.Append("sum(case WHEN type_id_usg in ('23126','23128')  THEN primary_units ELSE 0 END) usrsms ");
        //        //query.Append("from cdr where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");
        //        //query.Append("and date_part('month',trans_dt)= @param_month and date_part('year',trans_dt)= @param_year");
        //        //query.Append(" group by account_no,date_part('month',trans_dt),date_part('year',trans_dt),date_part('day',trans_dt) )p group by p.tp, p.ty, p.td");

        //        query.Append("select sum(p.usrdata) custdata,td labl from (select date_part('day',trans_dt) td,date_part('month',trans_dt) tp,");
        //        query.Append("date_part('year',trans_dt) ty,account_no,sum(primary_units) usrdata ");
        //        query.Append("from cdr_history where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");
        //        query.Append("and date_part('month',trans_dt)= @param_month and date_part('year',trans_dt)= @param_year");
        //        query.Append(" group by account_no,date_part('month',trans_dt),date_part('year',trans_dt),date_part('day',trans_dt) )p group by p.tp, p.ty, p.td");

        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<LineUsgVM>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_year = year,
        //                    param_month = month

        //                });

        //                lineUsgs = result.AsList();

        //                return lineUsgs;
        //            }
        //            catch (Exception)
        //            {
        //                return lineUsgs;
        //            }


        //        }

        //    }
        //    public async Task<List<SimDetailsVM>> GetSimDetails(string customer)
        //    {

        //        List<SimDetailsVM> simDetails = new List<SimDetailsVM>();

        //        StringBuilder query = new StringBuilder();
        //        query.Append("select circle_code,msisdn from esiminfo_new where circle_code = @param_circle and custname=  @param_customer");

        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<SimDetailsVM>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_circle = "KL"
        //                });

        //                simDetails = result.AsList();

        //                return simDetails;
        //            }
        //            catch (Exception)
        //            {
        //                return simDetails;
        //            }


        //        }

        //    }

        //    public async Task<List<SimDetailsVM>> SimsDownload(string customer, string circle)
        //    {

        //        List<SimDetailsVM> simDetails = new List<SimDetailsVM>();

        //        StringBuilder query = new StringBuilder();
        //        query.Append("select circle_code,msisdn,account_no,subscr_no,plan,service_status,service_active_dt from esiminfo_new where circle_code = @param_circle and custname=  @param_customer");

        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<SimDetailsVM>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_circle = circle
        //                });

        //                simDetails = result.AsList();

        //                return simDetails;
        //            }
        //            catch (Exception)
        //            {
        //                return simDetails;
        //            }


        //        }

        //    }

        //    public async Task<List<UsageDetailsVM>> UsageDownload(string customer, int filterYear, int filterMonth)
        //    {
        //        int sMonth = DateTime.Now.Month;
        //        int sYear = DateTime.Now.Year;
        //        List<UsageDetailsVM> usageDetails = new List<UsageDetailsVM>();

        //        StringBuilder query = new StringBuilder();
        //        query.Append("select point_origin msisdn, account_no, sum(primary_units) usrdata ");
        //        query.Append("from cdr_history where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");
        //        query.Append(" and date_part('month',trans_dt)= @param_month and date_part('year',trans_dt)= @param_year");
        //        query.Append(" group by point_origin,account_no");


        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<UsageDetailsVM>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_month = filterMonth,
        //                    param_year = filterYear,
        //                    param_circle = "KL"
        //                });

        //                usageDetails = result.AsList();

        //                return usageDetails;
        //            }
        //            catch (Exception)
        //            {
        //                return usageDetails;
        //            }


        //        }

        //    }
        //    public async Task<List<BillVM>> GetBillaccounts(string customer, string circle)
        //    {

        //        List<BillVM> billVMs = new List<BillVM>();

        //        StringBuilder query = new StringBuilder();
        //        query.Append("select distinct  account_no,to_char(entrydate,'DD-MM-YYYY') entrydate  from accountmaster   where custname=  @param_customer and circle_code = @param_circle order by entrydate desc");

        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<BillVM>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_circle = circle

        //                });

        //                billVMs = result.AsList();

        //                return billVMs;
        //            }
        //            catch (Exception)
        //            {
        //                return billVMs;
        //            }


        //        }

        //    }

        //    public async Task<SimDetailsDtVM> GetSimDetailsDatatable(string customer, string circle,string Simstatus, int start, int limit, string order, string dir, string search)
        //    {
        //        SimDetailsDtVM dtData = new SimDetailsDtVM();
        //        List<SimDetailsVM> data = new List<SimDetailsVM>();
        //        string simstatus=Simstatus;
        //        if(simstatus=="all")
        //        { Simstatus = "%"; }

        //        StringBuilder query = new StringBuilder();
        //        query.Append("select circle_code,msisdn,account_no,subscr_no,plan,service_status,service_active_dt ");
        //        query.Append(" from esiminfo_new");
        //        query.Append(" where circle_code = @param_circle and custname=  @param_customer and service_status like  @param_status");
        //        if (!String.IsNullOrEmpty(search))
        //        {
        //            query.Append(" and (msisdn like @param_search or service_status like @param_search or plan like @param_search)");
        //        }
        //        query.Append(" order by " + order + " " + dir + " limit @param_limit offset @param_start");

        //        StringBuilder queryCount = new StringBuilder();
        //        queryCount.Append("select count(*) ");
        //        queryCount.Append(" from esiminfo_new");
        //        queryCount.Append(" where circle_code = @param_circle and custname=  @param_customer and service_status like  @param_status");


        //        StringBuilder queryFilteredCount = new StringBuilder();
        //        queryFilteredCount.Append("select count(*) ");
        //        queryFilteredCount.Append(" from esiminfo_new");
        //        queryFilteredCount.Append("  where circle_code = @param_circle and custname=  @param_customer and service_status like  @param_status");
        //        if (!String.IsNullOrEmpty(search))
        //        {
        //            queryFilteredCount.Append(" and (msisdn like @param_search or service_status like @param_search or plan like @param_search)");
        //        }


        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var resultCount = await dbConnection.QueryAsync<int>(queryCount.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_circle = circle,
        //                    param_status = "%" + Simstatus + "%"
        //                });

        //                var totalRecords = resultCount.FirstOrDefault();
        //                var totalFilteredRecords = totalRecords;

        //                if (!String.IsNullOrEmpty(search))
        //                {
        //                    var resultFilteredCount = await dbConnection.QueryAsync<int>(queryFilteredCount.ToString(), new
        //                    {
        //                        param_customer = customer,
        //                        param_circle = circle,
        //                        param_status = "%" + Simstatus + "%",
        //                        param_search = "%" + search + "%"
        //                    });

        //                    totalFilteredRecords = resultFilteredCount.FirstOrDefault();
        //                }

        //                var result = await dbConnection.QueryAsync<SimDetailsVM>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_circle = circle,
        //                    param_status = "%" + Simstatus + "%",
        //                    param_start = start,
        //                    param_limit = limit,
        //                    param_search = "%" + search + "%"
        //                });

        //                data = result.AsList();

        //                dtData = new SimDetailsDtVM
        //                {
        //                    total_records = totalRecords,
        //                    total_filtered_records = totalFilteredRecords,
        //                    data = data
        //                };

        //                return dtData;
        //            }
        //            catch (Exception)
        //            {
        //                return dtData;
        //            }


        //        }

        //    }

        //    public async Task<UsageDetailsDtVM> GetUsageDetailsDatatable(string customer, string circle, int start, int limit, string order, string dir, string search)
        //    {
        //        int sMonth = DateTime.Now.Month;
        //        int sYear = DateTime.Now.Year;
        //        UsageDetailsDtVM dtData = new UsageDetailsDtVM();
        //        List<UsageDetailsVM> data = new List<UsageDetailsVM>();

        //        // ---------------- Main Query ----------------
        //        StringBuilder query = new StringBuilder();
        //        query.Append("select c.point_origin msisdn, c.account_no, sum(c.primary_units) usrdata, a.circle_code ");
        //        query.Append("from cdr_test c ");
        //        query.Append("join accountmaster a on c.account_no = a.account_no ");
        //        query.Append("where a.custname = @param_customer and a.circle_code = @param_circle ");

        //        if (!String.IsNullOrEmpty(search))
        //        {
        //            query.Append(" and (c.point_origin like @param_search or c.account_no like @param_search )");
        //        }

        //        query.Append(" and date_part('month',c.trans_dt)= @param_month and date_part('year',c.trans_dt)= @param_year");
        //        query.Append(" group by c.point_origin, c.account_no, a.circle_code ");
        //        query.Append(" order by " + order + " " + dir + " limit @param_limit offset @param_start");

        //        // ---------------- Count Query ----------------
        //        StringBuilder queryCount = new StringBuilder();
        //        queryCount.Append("select count(*) from (");
        //        queryCount.Append("select c.point_origin, c.account_no, sum(c.primary_units) usrdata, a.circle_code ");
        //        queryCount.Append("from cdr_test c ");
        //        queryCount.Append("join accountmaster a on c.account_no = a.account_no ");
        //        queryCount.Append("where a.custname = @param_customer and a.circle_code = @param_circle ");
        //        queryCount.Append(" and date_part('month',c.trans_dt)= @param_month and date_part('year',c.trans_dt)= @param_year ");
        //        queryCount.Append(" group by c.point_origin, c.account_no, a.circle_code ) p");

        //        // ---------------- Filtered Count Query ----------------
        //        StringBuilder queryFilteredCount = new StringBuilder();
        //        queryFilteredCount.Append("select count(*) from (");
        //        queryFilteredCount.Append("select c.point_origin, c.account_no, sum(c.primary_units) usrdata, a.circle_code ");
        //        queryFilteredCount.Append("from cdr_test c ");
        //        queryFilteredCount.Append("join accountmaster a on c.account_no = a.account_no ");
        //        queryFilteredCount.Append("where a.custname = @param_customer and a.circle_code = @param_circle ");

        //        if (!String.IsNullOrEmpty(search))
        //        {
        //            queryFilteredCount.Append(" and (c.point_origin like @param_search or c.account_no like @param_search)");
        //        }

        //        queryFilteredCount.Append(" and date_part('month',c.trans_dt)= @param_month and date_part('year',c.trans_dt)= @param_year ");
        //        queryFilteredCount.Append(" group by c.point_origin, c.account_no, a.circle_code ) p");

        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                // --- Total Count ---
        //                var resultCount = await dbConnection.QueryAsync<int>(queryCount.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_month = sMonth,
        //                    param_year = sYear,
        //                    param_circle = circle
        //                });

        //                var totalRecords = resultCount.FirstOrDefault();
        //                var totalFilteredRecords = totalRecords;

        //                // --- Filtered Count ---
        //                if (!String.IsNullOrEmpty(search))
        //                {
        //                    var resultFilteredCount = await dbConnection.QueryAsync<int>(queryFilteredCount.ToString(), new
        //                    {
        //                        param_customer = customer,
        //                        param_month = sMonth,
        //                        param_year = sYear,
        //                        param_circle = circle,
        //                        param_search = "%" + search + "%"
        //                    });

        //                    totalFilteredRecords = resultFilteredCount.FirstOrDefault();
        //                }

        //                // --- Data Query ---
        //                var result = await dbConnection.QueryAsync<UsageDetailsVM>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_month = sMonth,
        //                    param_year = sYear,
        //                    param_circle = circle,
        //                    param_start = start,
        //                    param_limit = limit,
        //                    param_search = "%" + search + "%"
        //                });

        //                data = result.AsList();

        //                dtData = new UsageDetailsDtVM
        //                {
        //                    total_records = totalRecords,
        //                    total_filtered_records = totalFilteredRecords,
        //                    data = data
        //                };

        //                return dtData;
        //            }
        //            catch (Exception)
        //            {
        //                return dtData;
        //            }
        //        }
        //    }


        //    public async Task<UsageDetailsDtVM> GetUsageDetailsDatatableHist(string customer, int filterYear, int filterMonth, int start, int limit, string order, string dir, string search)
        //    {
        //        int year = filterYear;
        //        int month = filterMonth;
        //        UsageDetailsDtVM dtData = new UsageDetailsDtVM();
        //        List<UsageDetailsVM> data = new List<UsageDetailsVM>();

        //        StringBuilder query = new StringBuilder();
        //        //query.Append("select point_origin msisdn, account_no, sum(CASE WHEN type_id_usg in ('23731', '23732')  THEN primary_units ");
        //        //query.Append("ELSE 0  END) usrdata,sum(CASE WHEN type_id_usg in ('23126', '23128')  THEN primary_units ");
        //        //query.Append("ELSE 0  END) usrsms from cdr where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");

        //        query.Append("select point_origin msisdn, account_no, sum(primary_units) usrdata ");
        //        query.Append("from cdr_history where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");
        //        if (!String.IsNullOrEmpty(search))
        //        {
        //            query.Append(" and (point_origin like @param_search or account_no like @param_search )");
        //        }
        //        query.Append(" and date_part('month',trans_dt)= @param_month and date_part('year',trans_dt)= @param_year");
        //        query.Append(" group by point_origin,account_no");
        //        query.Append(" order by " + order + " " + dir + " limit @param_limit offset @param_start");

        //        StringBuilder queryCount = new StringBuilder();
        //        //queryCount.Append("select count(*) from (");
        //        //queryCount.Append("select point_origin msisdn, account_no, sum(CASE WHEN type_id_usg in ('23731', '23732')  THEN primary_units ");
        //        //queryCount.Append("ELSE 0  END) usrdata,sum(CASE WHEN type_id_usg in ('23126', '23128')  THEN primary_units ");
        //        //queryCount.Append("ELSE 0  END) usrsms from cdr where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");
        //        //queryCount.Append(" and date_part('month',trans_dt)= @param_month and date_part('year',trans_dt)= @param_year");
        //        //queryCount.Append(" group by point_origin,account_no )p");
        //        queryCount.Append("select count(*) from (");
        //        queryCount.Append("select point_origin msisdn, account_no, sum(primary_units)  usrdata ");
        //        queryCount.Append("from cdr_test where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");
        //        queryCount.Append(" and date_part('month',trans_dt)= @param_month and date_part('year',trans_dt)= @param_year");
        //        queryCount.Append(" group by point_origin,account_no )p");


        //        StringBuilder queryFilteredCount = new StringBuilder();
        //        //queryFilteredCount.Append("select count(*) from (");
        //        //queryFilteredCount.Append("select point_origin msisdn, account_no, sum(CASE WHEN type_id_usg in ('23731', '23732')  THEN primary_units ");
        //        //queryFilteredCount.Append("ELSE 0  END) usrdata,sum(CASE WHEN type_id_usg in ('23126', '23128')  THEN primary_units ");
        //        //queryFilteredCount.Append("ELSE 0  END) usrsms from cdr where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");
        //        queryFilteredCount.Append("select count(*) from (");
        //        queryFilteredCount.Append("select point_origin msisdn, account_no, sum(primary_units)  usrdata ");
        //        queryFilteredCount.Append("from cdr_test where account_no in (select account_no from accountmaster a where a.custname = @param_customer)");
        //        if (!String.IsNullOrEmpty(search))
        //        {
        //            queryFilteredCount.Append(" and (point_origin like @param_search or account_no like @param_search)");
        //        }
        //        queryFilteredCount.Append(" and date_part('month',trans_dt)= @param_month and date_part('year',trans_dt)= @param_year");
        //        queryFilteredCount.Append(" group by point_origin,account_no )p");


        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var resultCount = await dbConnection.QueryAsync<int>(queryCount.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_year = year,
        //                    param_month = month,
        //                    param_circle = "KL"
        //                });

        //                var totalRecords = resultCount.FirstOrDefault();
        //                var totalFilteredRecords = totalRecords;

        //                if (!String.IsNullOrEmpty(search))
        //                {
        //                    var resultFilteredCount = await dbConnection.QueryAsync<int>(queryFilteredCount.ToString(), new
        //                    {
        //                        param_customer = customer,
        //                        param_year = year,
        //                        param_month = month,
        //                        param_circle = "KL",
        //                        param_search = "%" + search + "%"
        //                    });

        //                    totalFilteredRecords = resultFilteredCount.FirstOrDefault();
        //                }

        //                var result = await dbConnection.QueryAsync<UsageDetailsVM>(query.ToString(), new
        //                {
        //                    param_customer = customer,
        //                    param_year = year,
        //                    param_month = month,
        //                    param_circle = "KL",
        //                    param_start = start,
        //                    param_limit = limit,
        //                    param_search = "%" + search + "%"
        //                });

        //                data = result.AsList();

        //                dtData = new UsageDetailsDtVM
        //                {
        //                    total_records = totalRecords,
        //                    total_filtered_records = totalFilteredRecords,
        //                    data = data
        //                };

        //                return dtData;
        //            }
        //            catch (Exception)
        //            {
        //                return dtData;
        //            }


        //        }

        //    }

        //    public async Task<List<string>> GetYears()
        //    {

        //        StringBuilder query = new StringBuilder();
        //        query.Append("select distinct date_part('year',trans_dt) yr from cdr_history order by yr desc");

        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<string>(query.ToString());

        //                return result.AsList();
        //            }
        //            catch (Exception)
        //            {
        //                return new List<string>();
        //            }

        //        }

        //    }

        //    public async Task<List<int>> GetMonths(string filterYear)
        //    {


        //        StringBuilder query = new StringBuilder();
        //        query.Append("select distinct date_part('month',trans_dt) mnth from cdr_history where date_part('year', trans_dt) = @param_year  order by mnth desc");

        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<int>(query.ToString(), new
        //                {
        //                    param_year = Int32.Parse(filterYear)
        //                });

        //                return result.AsList();
        //            }
        //            catch (Exception er)
        //            {
        //                return new List<int>();
        //            }


        //        }

        //    }

        //    public async Task<LastupdVM> GetLastupd(string customer)
        //    {

        //        LastupdVM lastupd = new LastupdVM();

        //        StringBuilder query = new StringBuilder();
        //         query.Append("select  to_char(max (trans_dt),'MON DD, YYYY') Lastcdr from cdr_test");

        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<LastupdVM>(query.ToString(), new
        //                {
        //                   // param_customer = customer
        //                });

        //                return result.FirstOrDefault();

        //            }
        //            catch (Exception)
        //            {
        //                return lastupd;
        //            }


        //        }

        //    }

        //    public async Task<LastupdSimVM> GetLastupdSim(string customer)
        //    {

        //        LastupdSimVM lastupdsim = new LastupdSimVM();

        //        StringBuilder query = new StringBuilder();
        //        query.Append("select  to_char(max (entry_date),'MON DD, YYYY') LastSim from esiminfo_new");

        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();

        //            try
        //            {
        //                var result = await dbConnection.QueryAsync<LastupdSimVM>(query.ToString(), new
        //                {
        //                    // param_customer = customer
        //                });

        //                return result.FirstOrDefault();

        //            }
        //            catch (Exception)
        //            {
        //                return lastupdsim;
        //            }


        //        }

        //    }
        //    //Get the count of sims under each plan
        //    public async Task<dynamic> GetPlanWiseCount(string customer)
        //    {
        //        StringBuilder query = new StringBuilder();
        //        query.Append(@"
        //            select display_value, b.package_id, cnt from
        //            (select display_value, package_id::text from m2m_plans order by package_id) a
        //            right join
        //            (select coalesce(exist_package_id, 'NA') package_id, count(*) cnt from esiminfo_new where custname=:param_customer group by exist_package_id) b
        //            on a.package_id=b.package_id
        //            union
        //            select 'Disconnected sims' as display_value, 'DISC' as package_id, count(*) from disconnected_sims where custname=:param_customer
        //        ");
        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();
        //            try
        //            {
        //                var result = await dbConnection.QueryAsync(query.ToString(), new
        //                {
        //                    param_customer = customer
        //                });
        //                return result.AsList();
        //            }
        //            catch (Exception)
        //            {
        //                throw;
        //            }
        //        }
        //    }
        //    //Get the details of sims under each Plan
        //    public async Task<dynamic> GetPlanWiseDetails(string customer, string plan_id)
        //    {
        //        StringBuilder query = new StringBuilder();
        //        if (plan_id != "DISC")
        //        {
        //            query.Append("select * from esiminfo_new where custname=:param_customer ");
        //            if (plan_id == "NA")
        //            {
        //                query.Append("and exist_package_id is null ");
        //            }
        //            else
        //            {
        //                query.Append("and exist_package_id=:param_plan_id ");
        //            }
        //        }
        //        else
        //        {
        //            query.Append("select * from disconnected_sims where custname=:param_customer ");
        //        }
        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();
        //            try
        //            {
        //                var result = await dbConnection.QueryAsync(query.ToString(), new
        //                {
        //                    param_plan_id = plan_id,
        //                    param_customer = customer
        //                });
        //                return result.AsList();
        //            }
        //            catch (Exception)
        //            {
        //                throw;
        //            }
        //        }
        //    }
        //    //Get the count of sims under each account of the customer
        //    public async Task<dynamic> GetAccountWiseCount(string customer)
        //    {
        //        StringBuilder query = new StringBuilder();
        //        query.Append("select account_no, count(*) cnt from esiminfo_new where custname=:param_customer group by account_no order by account_no");
        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();
        //            try
        //            {
        //                var result = await dbConnection.QueryAsync(query.ToString(), new
        //                {
        //                    param_customer = customer
        //                });
        //                return result.AsList();
        //            }
        //            catch (Exception)
        //            {
        //                throw;
        //            }
        //        }
        //    }
        //    //Get the details of sims under each account
        //    public async Task<dynamic> GetAccountWiseDetails(string customer, string account_no)
        //    {
        //        StringBuilder query = new StringBuilder();
        //        query.Append("select * from esiminfo_new where custname=:param_customer and account_no=:param_account_no");
        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();
        //            try
        //            {
        //                var result = await dbConnection.QueryAsync(query.ToString(), new
        //                {
        //                    param_account_no = account_no,
        //                    param_customer = customer
        //                });
        //                return result.AsList();
        //            }
        //            catch (Exception)
        //            {
        //                throw;
        //            }
        //        }
        //    }
        //    //Get sms count
        //    public async Task<dynamic> GetSmsusage(string customer, string role)
        //    {
        //        StringBuilder query = new StringBuilder();
        //        if (role == "customer")
        //        {
        //            query.Append("select b.account_no,b.msisdn,b.sms_count,TO_CHAR(b.entry_date::date, 'DD-MM-YY') AS entry_date from (select account_no from accountmaster where custname=:param_customer) a join sms_count b on a.account_no = b.account_no");
        //        }
        //        else
        //        {
        //            //to be modified for circle admin
        //            query.Append("select * from (select account_no from accountmaster where circle_code=(select circle from users where mobile=:param_customer)) a join outstanding_dues b on a.account_no = b.account_no ");
        //        }
        //        //query.Append("select account_no, round(amount, 2) amount from outstanding_dues ");
        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();
        //            try
        //            {
        //                var result = await dbConnection.QueryAsync(query.ToString(), new
        //                {
        //                    param_customer = customer
        //                });
        //                return result.AsList();
        //            }
        //            catch (Exception)
        //            {
        //                throw;
        //            }
        //        }
        //    }

        //    //get disconnected sim details for wz
        //    public async Task<IEnumerable<dynamic>> GetDiscsimdetails(string customer)
        //    {
        //        var query = @"
        //    SELECT 
        //        circle,
        //        msisdn,
        //        account_no,
        //        subscr_no,
        //        TO_CHAR(active_date, 'DD-Mon-YYYY') AS active_date,
        //        TO_CHAR(inactive_date, 'DD-Mon-YYYY') AS inactive_date
        //    FROM disconnected_sims_wz
        //    WHERE custname = :param_customer
        //    ORDER BY account_no limit 10";

        //        using (IDbConnection dbConnection = ConnectionPgSql)
        //        {
        //            dbConnection.Open();
        //            try
        //            {
        //                var result = await dbConnection.QueryAsync(query, new { param_customer = customer });
        //                return result;
        //            }
        //            catch (Exception ex)
        //            {
        //                throw new Exception("Error fetching disconnected sim details", ex);
        //            }
        //        }
        //    }

        //    //server side method
        //    public async Task<(IEnumerable<dynamic> Data, int Total, int Filtered)>
        //GetDiscsimdetailsServer(string customer, string search, int skip, int pageSize)
        //    {
        //        using (IDbConnection db = ConnectionPgSql)
        //        {
        //            db.Open();

        //            // Base query
        //            var baseQuery = @"
        //        FROM disconnected_sims_wz
        //        WHERE custname = :param_customer";

        //            // Add search condition if provided
        //            if (!string.IsNullOrEmpty(search))
        //            {
        //                baseQuery += " AND (msisdn ILIKE :search OR account_no ILIKE :search OR subscr_no ILIKE :search)";
        //            }

        //            // Count total
        //            var totalQuery = $"SELECT COUNT(*) {baseQuery}";
        //            var total = await db.ExecuteScalarAsync<int>(totalQuery, new
        //            {
        //                param_customer = customer,
        //                search = $"%{search}%"
        //            });

        //            // Fetch paged data
        //            var dataQuery = $@"
        //        SELECT 
        //            circle,
        //            msisdn,
        //            account_no,
        //            subscr_no,
        //            TO_CHAR(active_date, 'DD-Mon-YYYY') AS active_date,
        //            TO_CHAR(inactive_date, 'DD-Mon-YYYY') AS inactive_date
        //        {baseQuery}
        //        ORDER BY account_no
        //        LIMIT :pageSize OFFSET :skip";

        //            var data = await db.QueryAsync(dataQuery, new
        //            {
        //                param_customer = customer,
        //                search = $"%{search}%",
        //                pageSize,
        //                skip
        //            });

        //            return (data, total, total);
        //        }
        //    }


    }
}
