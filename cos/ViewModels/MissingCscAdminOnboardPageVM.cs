using System.Collections.Generic;

namespace cos.ViewModels
{
    public class MissingCscAdminOnboardPageVM
    {
        public IEnumerable<CircleOptionVM> Circles { get; set; } = new List<CircleOptionVM>();
        public IEnumerable<SsaOptionVM> Ssas { get; set; } = new List<SsaOptionVM>();
        public CtopMasterCreateVM NewCtop { get; set; } = new CtopMasterCreateVM();
        public string? ZoneCode { get; set; }
        public string? CircleCode { get; set; }
        public string? SsaCode { get; set; }
    }
}

