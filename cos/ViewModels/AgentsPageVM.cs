using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace cos.ViewModels
{
    public class AgentsPageVM
    {
        public CtopMaster? Retailer { get; set; }
        public string? RetailerCtopupno { get; set; }
        public IEnumerable<CircleOptionVM> Circles { get; set; } = new List<CircleOptionVM>();
        public IEnumerable<SsaOptionVM> Ssas { get; set; } = new List<SsaOptionVM>();
        public CtopMasterCreateVM NewAgent { get; set; } = new CtopMasterCreateVM();
    }

    public class GetAgentsRequest
    {
        public string? retailerCtopupno { get; set; }
        public int? draw { get; set; }
        public int? start { get; set; }
        public int? length { get; set; }
        public DataTableSearch? search { get; set; }
    }

    public class DataTableSearch
    {
        public string? value { get; set; }
        public bool? regex { get; set; }
    }
}

