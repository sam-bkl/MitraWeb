using System.Threading.Tasks;
using cos.ViewModels;

public interface IPgMasterRepository
{
    Task<CmfMasterVM> GetCmfMasterByCircleAsync(string circle);
}

