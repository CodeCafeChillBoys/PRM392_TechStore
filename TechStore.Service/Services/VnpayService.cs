using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    /// VNPay Sandbox integration — v2.1.0
    /// Signing spec: https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.md
    ///
    /// CRITICAL signing rule:
    ///   Sign string  = sorted key=WebUtility.UrlEncode(value) pairs joined by '&'
    ///   (spaces → '+', special chars → %XX uppercase, same as PHP urlencode)
    ///   This is what VNPay's backend uses both for VERIFYING the payment request
    ///   and for BUILDING the return/IPN callback.
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

            // VNPay only accepts IPv4 — map IPv6 loopback to 127.0.0.1
            var ip = (ipAddress == "::1" || ipAddress == "0:0:0:0:0:0:0:1")
                ? "127.0.0.1"
                : ipAddress;

            // SortedDictionary ensures alphabetical ordering required for signing
            var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Version"]    = "2.1.0",
                ["vnp_Command"]    = "pay",
                ["vnp_TmnCode"]    = _settings.TmnCode,
                ["vnp_Amount"]     = amount,
                ["vnp_CreateDate"] = vnNow.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"]   = "VND",
                ["vnp_IpAddr"]     = ip,
                ["vnp_Locale"]     = "vn",
                ["vnp_OrderInfo"]  = $"Thanh toan don hang {order.Id}",
                ["vnp_OrderType"]  = "other",
                ["vnp_ReturnUrl"]  = _settings.ReturnUrl,
                ["vnp_TxnRef"]     = order.Id.ToString(),
                ["vnp_ExpireDate"] = vnNow.AddMinutes(15).ToString("yyyyMMddHHmmss"),
            };

            // ── Sign: WebUtility.UrlEncode values (spaces → '+'), matching VNPay's PHP backend ──
            var signData   = BuildSignData(vnpParams);
            var secureHash = HmacSha512(_settings.HashSecret, signData);

            // ── URL: also use WebUtility.UrlEncode for consistency ──
            var queryString = BuildUrlQuery(vnpParams);

            return $"{_settings.PaymentUrl}?{queryString}&vnp_SecureHash={secureHash}";
        }

        // ─────────────────────────────────────────────────────────────────────
        // VALIDATE SIGNATURE  (IPN / Return URL)
        // ─────────────────────────────────────────────────────────────────────
        // VNPay builds the return/IPN URL using the same WebUtility.UrlEncode approach.
        // ASP.NET Core's IQueryCollection auto URL-decodes (+ → space, %20 → space),
        // losing the encoding info needed to reproduce VNPay's sign string.
        // Solution: pass Request.QueryString.Value (raw string) so we keep the
        // original encoded values (e.g. '+') and reproduce the exact same sign string.
        public bool ValidateSignature(
            string rawQueryString,
            out string responseCode,
            out string transactionId,
            out string orderId)
        {
            responseCode  = string.Empty;
            transactionId = string.Empty;
            orderId       = string.Empty;

            if (string.IsNullOrEmpty(rawQueryString))
                return false;

            // Strip leading '?'
            if (rawQueryString.StartsWith('?'))
                rawQueryString = rawQueryString[1..];

            string receivedHash = string.Empty;
            // Store decoded key → raw (encoded) value for sign reproduction
            var paramDict = new SortedDictionary<string, string>(StringComparer.Ordinal);

            foreach (var pair in rawQueryString.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = pair.IndexOf('=');
                if (idx < 0) continue;

                var rawKey   = pair[..idx];
                var rawValue = pair[(idx + 1)..];
                var key      = Uri.UnescapeDataString(rawKey);

                if (key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase))
                {
                    receivedHash = Uri.UnescapeDataString(rawValue);
                    continue;
                }
                if (key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                    continue;

                paramDict[key] = rawValue; // keep raw (encoded) value
            }

            // Rebuild output params (decoded for caller use)
            responseCode  = Decode(paramDict, "vnp_ResponseCode");
            transactionId = Decode(paramDict, "vnp_TransactionNo");
            orderId       = Decode(paramDict, "vnp_TxnRef");

            // Reproduce VNPay's sign string from decoded key + raw (encoded) value
            var signData     = string.Join("&", paramDict.Select(kv => $"{kv.Key}={kv.Value}"));
            var expectedHash = HmacSha512(_settings.HashSecret, signData);

            return expectedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase);
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Build sign string: key=WebUtility.UrlEncode(value) joined by '&'.
        /// Spaces become '+', special chars become %XX (uppercase).
        /// This matches VNPay's PHP urlencode() signing algorithm.
        /// </summary>
        private static string BuildSignData(SortedDictionary<string, string> p)
            => string.Join("&", p.Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));

        /// <summary>
        /// Build URL query string using the same WebUtility.UrlEncode encoding
        /// so the URL params stay consistent with the sign string.
        /// </summary>
        private static string BuildUrlQuery(SortedDictionary<string, string> p)
            => string.Join("&", p.Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));

        /// <summary>Safely URL-decode a value from the raw param dict.</summary>
        private static string Decode(SortedDictionary<string, string> d, string key)
            => d.TryGetValue(key, out var v) ? Uri.UnescapeDataString(v.Replace("+", " ")) : string.Empty;

        /// <summary>HMAC-SHA512 — always 128 hex chars (BitConverter preserves leading zeros).</summary>
        private static string HmacSha512(string key, string data)
        {
            var keyBytes  = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA512(keyBytes);
            return BitConverter.ToString(hmac.ComputeHash(dataBytes))
                               .Replace("-", string.Empty)
                               .ToLowerInvariant();
        }
    }
}
