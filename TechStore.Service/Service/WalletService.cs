using System.Data;
using Microsoft.EntityFrameworkCore;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Enum;
using TechStore.Domain.Models;
using TechStore.Repository.Data;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class WalletService : IWalletService
    {
        public const decimal MinimumWithdrawalAmount = 50_000m;

        private readonly ApplicationDbContext _context;
        private readonly IVnpayService _vnpayService;

        public WalletService(ApplicationDbContext context, IVnpayService vnpayService)
        {
            _context = context;
            _vnpayService = vnpayService;
        }

        public async Task<WalletResponseDTO> GetWalletAsync(Guid userId)
        {
            var wallet = await GetOrCreateWalletAsync(userId);
            return MapWallet(wallet);
        }

        public async Task<IReadOnlyList<WalletTransactionResponseDTO>> GetTransactionsAsync(
            Guid userId,
            int skip,
            int take)
        {
            var wallet = await GetOrCreateWalletAsync(userId);

            return await _context.WalletTransactions
                .AsNoTracking()
                .Where(transaction => transaction.WalletId == wallet.Id)
                .OrderByDescending(transaction => transaction.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(transaction => new WalletTransactionResponseDTO
                {
                    Id = transaction.Id,
                    Type = transaction.Type,
                    Status = transaction.Status,
                    Amount = transaction.Amount,
                    BalanceBefore = transaction.BalanceBefore,
                    BalanceAfter = transaction.BalanceAfter,
                    Description = transaction.Description,
                    VnpayTransactionId = transaction.VnpayTransactionId,
                    OrderId = transaction.OrderId,
                    BankName = transaction.BankName,
                    BankAccountNumber = transaction.BankAccountNumber,
                    AccountHolderName = transaction.AccountHolderName,
                    CreatedAt = transaction.CreatedAt,
                    ProcessedAt = transaction.ProcessedAt
                })
                .ToListAsync();
        }

        public async Task<WalletTopUpResponseDTO> CreateTopUpAsync(
            Guid userId,
            decimal amount,
            string ipAddress)
        {
            if (amount <= 0 || decimal.Round(amount, 0) != amount)
                throw new InvalidOperationException("Số tiền nạp phải là số nguyên dương (VND).");

            var wallet = await GetOrCreateWalletAsync(userId);
            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = WalletTransactionType.TopUp,
                Status = WalletTransactionStatus.Pending,
                Amount = amount,
                Description = "Nạp tiền vào ví qua VNPay",
                CreatedAt = DateTime.UtcNow
            };

            await _context.WalletTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            return new WalletTopUpResponseDTO
            {
                TransactionId = transaction.Id,
                Amount = transaction.Amount,
                PaymentUrl = _vnpayService.CreatePaymentUrl(
                    transaction.Id,
                    transaction.Amount,
                    $"Nap tien vi TechStore {transaction.Id}",
                    ipAddress)
            };
        }

        public async Task<WalletTransactionResponseDTO> WithdrawAsync(
            Guid userId,
            WalletWithdrawalRequest request)
        {
            if (request.Amount < MinimumWithdrawalAmount || decimal.Round(request.Amount, 0) != request.Amount)
                throw new InvalidOperationException("Số tiền rút tối thiểu là 50.000đ và phải là số nguyên VND.");

            if (string.IsNullOrWhiteSpace(request.BankName) ||
                string.IsNullOrWhiteSpace(request.BankAccountNumber) ||
                string.IsNullOrWhiteSpace(request.AccountHolderName))
            {
                throw new InvalidOperationException("Thông tin tài khoản nhận tiền không được để trống.");
            }

            await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            var wallet = await _context.Wallets
                .FromSqlInterpolated($"SELECT * FROM \"Wallets\" WHERE \"UserId\" = {userId} FOR UPDATE")
                .SingleOrDefaultAsync();

            if (wallet == null)
                throw new InvalidOperationException("Không tìm thấy ví của người dùng.");

            if (wallet.Balance <= MinimumWithdrawalAmount)
                throw new InvalidOperationException("Số dư ví phải lớn hơn 50.000đ mới có thể rút tiền.");

            if (wallet.Balance < request.Amount)
                throw new InvalidOperationException("Số dư ví không đủ để thực hiện giao dịch.");

            var before = wallet.Balance;
            wallet.Balance -= request.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = WalletTransactionType.Withdrawal,
                Status = WalletTransactionStatus.Pending,
                Amount = request.Amount,
                BalanceBefore = before,
                BalanceAfter = wallet.Balance,
                Description = "Yêu cầu rút tiền từ ví",
                BankName = request.BankName.Trim(),
                BankAccountNumber = request.BankAccountNumber.Trim(),
                AccountHolderName = request.AccountHolderName.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.WalletTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return MapTransaction(transaction);
        }

        public async Task<TopUpConfirmationStatus> ConfirmTopUpAsync(
            Guid transactionId,
            bool success,
            string vnpayTransactionId,
            decimal paidAmount)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            var transaction = await _context.WalletTransactions
                .FromSqlInterpolated($"SELECT * FROM \"WalletTransactions\" WHERE \"Id\" = {transactionId} FOR UPDATE")
                .SingleOrDefaultAsync();

            if (transaction == null || transaction.Type != WalletTransactionType.TopUp)
                return TopUpConfirmationStatus.NotFound;

            if (transaction.Amount != paidAmount)
                return TopUpConfirmationStatus.AmountMismatch;

            if (transaction.Status == WalletTransactionStatus.Completed)
                return TopUpConfirmationStatus.AlreadyProcessed;

            if (transaction.Status == WalletTransactionStatus.Failed)
                return TopUpConfirmationStatus.Failed;

            if (!success)
            {
                transaction.Status = WalletTransactionStatus.Failed;
                transaction.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return TopUpConfirmationStatus.Failed;
            }

            if (string.IsNullOrWhiteSpace(vnpayTransactionId))
                throw new InvalidOperationException("VNPay không trả về mã giao dịch.");

            var duplicateTransaction = await _context.WalletTransactions
                .AnyAsync(item => item.Id != transaction.Id &&
                                  item.VnpayTransactionId == vnpayTransactionId);
            if (duplicateTransaction)
                throw new InvalidOperationException("Mã giao dịch VNPay đã được sử dụng.");

            var wallet = await _context.Wallets
                .FromSqlInterpolated($"SELECT * FROM \"Wallets\" WHERE \"Id\" = {transaction.WalletId} FOR UPDATE")
                .SingleAsync();

            var before = wallet.Balance;
            wallet.Balance += transaction.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            transaction.Status = WalletTransactionStatus.Completed;
            transaction.BalanceBefore = before;
            transaction.BalanceAfter = wallet.Balance;
            transaction.VnpayTransactionId = vnpayTransactionId;
            transaction.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return TopUpConfirmationStatus.Completed;
        }

        private async Task<Wallet> GetOrCreateWalletAsync(Guid userId)
        {
            var wallet = await _context.Wallets.SingleOrDefaultAsync(item => item.UserId == userId);
            if (wallet != null)
                return wallet;

            if (!await _context.Users.AnyAsync(user => user.Id == userId))
                throw new InvalidOperationException("Không tìm thấy người dùng.");

            wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Wallets.AddAsync(wallet);
            await _context.SaveChangesAsync();
            return wallet;
        }

        private static WalletResponseDTO MapWallet(Wallet wallet) => new()
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            Balance = wallet.Balance,
            UpdatedAt = wallet.UpdatedAt
        };

        private static WalletTransactionResponseDTO MapTransaction(WalletTransaction transaction) => new()
        {
            Id = transaction.Id,
            Type = transaction.Type,
            Status = transaction.Status,
            Amount = transaction.Amount,
            BalanceBefore = transaction.BalanceBefore,
            BalanceAfter = transaction.BalanceAfter,
            Description = transaction.Description,
            VnpayTransactionId = transaction.VnpayTransactionId,
            OrderId = transaction.OrderId,
            BankName = transaction.BankName,
            BankAccountNumber = transaction.BankAccountNumber,
            AccountHolderName = transaction.AccountHolderName,
            CreatedAt = transaction.CreatedAt,
            ProcessedAt = transaction.ProcessedAt
        };
    }
}
