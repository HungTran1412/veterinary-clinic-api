using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Text.Json;

namespace VeterinaryClinic.API.Localization
{
    public class JsonStringLocalizer : IStringLocalizer
    {
        private Dictionary<string, string> _resources;
        private string _currentCulture;

        public JsonStringLocalizer()
        {
            _resources = new Dictionary<string, string>();
            _currentCulture = CultureInfo.CurrentUICulture.Name;
            LoadResources(_currentCulture);
        }

        private void LoadResources(string culture)
        {
            _resources.Clear();
            
            // 1. Thử load file theo culture hiện tại
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "Localization", $"{culture}.json");
            
            if (!File.Exists(filePath))
            {
                // 2. Fallback về vi-VN nếu không tìm thấy
                filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "Localization", "vi-VN.json");
            }

            if (File.Exists(filePath))
            {
                try 
                {
                    var json = File.ReadAllText(filePath);
                    _resources = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }
                catch
                {
                    // Log lỗi hoặc bỏ qua
                    _resources = new Dictionary<string, string>();
                }
            }
        }

        private string GetString(string name)
        {
            // Kiểm tra xem culture có thay đổi không (vì Localizer thường là Singleton hoặc Scoped nhưng Culture thay đổi theo Request)
            if (CultureInfo.CurrentUICulture.Name != _currentCulture)
            {
                _currentCulture = CultureInfo.CurrentUICulture.Name;
                LoadResources(_currentCulture);
            }

            if (_resources.TryGetValue(name, out var value))
            {
                return value;
            }
            
            // Nếu không tìm thấy trong file hiện tại, thử tìm trong vi-VN (Fallback cứng)
            // (Phần này có thể tối ưu thêm, nhưng hiện tại giữ đơn giản)
            
            return null;
        }

        public LocalizedString this[string name]
        {
            get
            {
                var value = GetString(name);
                return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var format = GetString(name);
                var value = string.Format(format ?? name, arguments);
                return new LocalizedString(name, value, resourceNotFound: format == null);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return _resources.Select(r => new LocalizedString(r.Key, r.Value, true));
        }
    }

    public class JsonStringLocalizerFactory : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource)
        {
            return new JsonStringLocalizer();
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            return new JsonStringLocalizer();
        }
    }
}
