using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace VeterinaryClinic.Business
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _requestData[key] = value;
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _responseData[key] = value;
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var data = BuildData(_requestData, includeHash: false);
            var secureHash = HmacSha512(hashSecret, data);
            return $"{baseUrl}?{data}&vnp_SecureHash={secureHash}";
        }

        public bool ValidateSignature(string secureHash, string hashSecret)
        {
            var data = BuildData(_responseData, includeHash: false);
            var checkSum = HmacSha512(hashSecret, data);
            return string.Equals(checkSum, secureHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildData(SortedList<string, string> data, bool includeHash)
        {
            var query = new StringBuilder();
            foreach (var item in data)
            {
                if (!includeHash &&
                    (item.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) ||
                     item.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (query.Length > 0)
                {
                    query.Append('&');
                }

                query.Append(WebUtility.UrlEncode(item.Key));
                query.Append('=');
                query.Append(WebUtility.UrlEncode(item.Value));
            }

            return query.ToString();
        }

        private static string HmacSha512(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using var hmac = new HMACSHA512(keyBytes);
            var hashValue = hmac.ComputeHash(inputBytes);
            return string.Concat(hashValue.Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private class VnPayCompare : IComparer<string>
        {
            public int Compare(string? x, string? y)
            {
                return string.CompareOrdinal(x, y);
            }
        }
    }
}
