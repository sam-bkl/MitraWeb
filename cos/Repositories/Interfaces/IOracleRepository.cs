using cos.ViewModels;

namespace YourProject.Repositories.Interfaces
{
    public interface IOracleRepository
    {
        Task<List<BcdVM>> GetBcdDetails();
        Task<string> TestOracleConnection();

        // Task<bool> InsertBcdRecordAsync(string gsmNumber, string cafSerial, string userId);

        Task<bool> InsertBcdFromCafAsync(CafModel model, string verifiedBy);
        Task<bool> InsertBcdFromCafSwapAsync(CafSwapModel model, string verifiedBy);

        Task<bool> InsertSimSwapAmountDeductRequestAsync(CafSwapModel model, decimal amount, string userId);


    }
}
