using Npgsql;
using System.Threading.Tasks;
using cos.ViewModels;

public class PgMasterRepository : IPgMasterRepository
{
    private readonly IConfiguration _config;

    public PgMasterRepository(IConfiguration config)
    {
        _config = config;
    }

    public async Task<CmfMasterVM> GetCmfMasterByCircleAsync(string circle)
    {
        string connStr = _config.GetConnectionString("PgDb");

        using var con = new NpgsqlConnection(connStr);
        await con.OpenAsync();

        string sql = @"SELECT * FROM cmfmaster_sz WHERE circle = @circle LIMIT 1";

        using var cmd = new NpgsqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@circle", circle);

        using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new CmfMasterVM
            {
                Circle = reader["circle"]?.ToString(),
                Circle_Code = reader["circle_code"] as int?,
                Act_Type = reader["act_type"] as int?,
                Connection_Type = reader["connection_type"] as int?,
                Msisdn_Type = reader["msisdn_type"] as int?,
                Sim_Type = reader["sim_type"] as int?,
                Cmf_Mkt_Code = reader["cmf_mkt_code"] as int?,
                Zone_Element_Id = reader["zone_element_id"] as int?,
                Cmf_Vip_Code = reader["cmf_vip_code"] as int?,
                Cmf_Acct_Seg_Id = reader["cmf_acct_seg_id"] as int?,
                Cmf_Exrate_Class = reader["cmf_exrate_class"] as int?,
                Cmf_Rate_Class_Default = reader["cmf_rate_class_default"] as int?,
                Cmf_Bill_Disp_Meth = reader["cmf_bill_disp_meth"] as int?,
                Cmf_Bill_Fmt_Opt = reader["cmf_bill_fmt_opt"] as int?,
                Invs_Saleschannel_Id = reader["invs_saleschannel_id"] as int?,
                Emf_Config_Id = reader["emf_config_id"] as int?,
                Zone_Id = reader["zone_id"] as int?,
                Cmf_Bill_Period = reader["cmf_bill_period"]?.ToString()
            };
        }

        return null;
    }
}
