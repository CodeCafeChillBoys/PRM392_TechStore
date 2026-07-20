namespace TechStore.Service.IService
{
    public interface IVnpayService
    {
        /// <summary>
        /// Builds a signed VNPay payment URL for a wallet top-up transaction.
        /// </summary>
        /// <param name="transactionReference">Wallet transaction ID used as vnp_TxnRef.</param>
        /// <param name="amount">Top-up amount in VND.</param>
        /// <param name="description">VNPay order information.</param>
        /// <param name="ipAddress">Client IP address (required by VNPay).</param>
        /// <returns>Full payment URL including HMAC-SHA512 signature.</returns>
        string CreatePaymentUrl(
            Guid transactionReference,
            decimal amount,
            string description,
            string ipAddress);

        /// <summary>
        /// Validates the HMAC-SHA512 signature on the VNPay callback query string.
        /// Pass Request.QueryString.Value (the RAW, URL-encoded query string) so the
        /// hash is computed over the same encoded representation VNPay uses.
        /// </summary>
        /// <param name="rawQueryString">Raw query string from Request.QueryString.Value (e.g. "?vnp_Amount=...&vnp_SecureHash=...").</param>
        /// <param name="responseCode">vnp_ResponseCode: "00" = success.</param>
        /// <param name="vnpayTransactionId">vnp_TransactionNo returned by VNPay.</param>
        /// <param name="transactionReference">vnp_TxnRef = our WalletTransaction.Id as string.</param>
        /// <returns>True if the signature is valid (not tampered).</returns>
        bool ValidateSignature(
            string rawQueryString,
            out string responseCode,
            out string vnpayTransactionId,
            out string transactionReference);
    }
}
