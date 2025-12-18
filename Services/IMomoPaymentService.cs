using System.Threading.Tasks;

namespace WasteCollectionSystem.Services
{
    /// <summary>
    /// Abstraction for MTN MoMo payment operations (DI-friendly, testable).
    /// </summary>
    public interface IMomoPaymentService
    {
        Task<string> RequestToPayAsync(string phone, decimal amount);
        Task<string> GetTokenAsync();
        Task<MtnMomoService.MomoTransactionResult> GetTransactionStatusAsync(string transactionId);
    }
}

