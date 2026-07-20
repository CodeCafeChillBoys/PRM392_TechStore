using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Enum;

namespace TechStore.Service.IService
{
    public interface IWalletService
    {
        Task<WalletResponseDTO> GetWalletAsync(Guid userId);

        Task<IReadOnlyList<WalletTransactionResponseDTO>> GetTransactionsAsync(
            Guid userId,
            int skip,
            int take);

        Task<WalletTopUpResponseDTO> CreateTopUpAsync(
            Guid userId,
            decimal amount,
            string ipAddress);

        Task<WalletTransactionResponseDTO> WithdrawAsync(
            Guid userId,
            WalletWithdrawalRequest request);

        Task<TopUpConfirmationStatus> ConfirmTopUpAsync(
            Guid transactionId,
            bool success,
            string vnpayTransactionId,
            decimal paidAmount);
    }
}
