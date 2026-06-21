using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TechStore.Domain.Models;
using TechStore.Domain.Settings;
using TechStore.Service.IServices;

namespace TechStore.Service.Services
{
    /// <summary>
    /// Implements VNPay Sandbox integration.
    /// Reference: https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.md
    /// </summary>
    public class VnpayService : IVnpayService
    {
        private readonly VnpaySettings _settings;

        public VnpayService(IOptions<VnpaySettings> settings)
        {
            _settings = settings.Value;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE PAYMENT URL
        // ─────────────────────────────────────────────────────────────────────
        public string CreatePaymentUrl(Order order, string ipAddress)
        {
            // VNPay requires Vietnam time (UTC+7)
            var vnTimeZoneId = OperatingSystem.IsWindows()
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh";
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById(vnTimeZoneId);
            var vnNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);

            // Amount must be multiplied by 100 (VNPay uses integer, no decimal)
            var amount = ((long)(order.TotalAmount * 100)).ToString();

            // SortedDictionary ensures alphabetical ordering required for signing
            var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Version"]    = "2.1.0",
                ["vnp_Command"]    = "pay",
                ["vnp_TmnCode"]    = _settings.TmnCode,
                ["vnp_Amount"]     = amount,
                ["vnp_CreateDate"] = vnNow.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"]   = "VND",
                ["vnp_IpAddr"]     = ipAddress,
                ["vnp_Locale"]     = "vn",
                ["vnp_OrderInfo"]  = $"Thanh toan don hang {order.Id}",
                ["vnp_OrderType"]  = "other",
                ["vnp_ReturnUrl"]  = _settings.ReturnUrl,
                ["vnp_TxnRef"]     = order.Id.ToString(),
                ["vnp_ExpireDate"] = vnNow.AddMinutes(15).ToString("yyyyMMddHHmmss"),
            };

            // Build raw sign data (no URL encoding) then sign
            var signData   = BuildRawQueryString(vnpParams);
            var secureHash = HmacSha512(_settings.HashSecret, signData);

            // Build URL-encoded query string for the payment URL
            var encodedQuery = BuildEncodedQueryString(vnpParams);

            return $"{_settings.PaymentUrl}?{encodedQuery}&vnp_SecureHash={secureHash}";
        }

        // ─────────────────────────────────────────────────────────────────────
        // VALIDATE SIGNATURE  (IPN / Return URL)
        // ─────────────────────────────────────────────────────────────────────
        public bool ValidateSignature(
            IQueryCollection query,
            out string responseCode,
            out string transactionId,
            out string orderId)
        {
            responseCode  = query["vnp_ResponseCode"].ToString();
            transactionId = query["vnp_TransactionNo"].ToString();
            orderId       = query["vnp_TxnRef"].ToString();

            var receivedHash = query["vnp_SecureHash"].ToString();

            // Rebuild params exactly as VNPay sent them, excluding hash fields
            var paramDict = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in query)
            {
                if (key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                 || key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                    continue;

                paramDict[key] = value.ToString();
            }

            var signData     = BuildRawQueryString(paramDict);
            var expectedHash = HmacSha512(_settings.HashSecret, signData);

            return expectedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase);
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Builds raw (non-encoded) key=value&amp;... string for HMAC signing.</summary>
        private static string BuildRawQueryString(SortedDictionary<string, string> parameters)
            => string.Join("&", parameters.Select(kv => $"{kv.Key}={kv.Value}"));

        /// <summary>Builds URL-encoded key=value&amp;... string for the payment URL.</summary>
        private static string BuildEncodedQueryString(SortedDictionary<string, string> parameters)
            => string.Join("&", parameters.Select(
                kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

        /// <summary>Computes HMAC-SHA512 and returns lowercase hex string.</summary>
        private static string HmacSha512(string key, string data)
        {
            var keyBytes  = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA512(keyBytes);
            var hash = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
