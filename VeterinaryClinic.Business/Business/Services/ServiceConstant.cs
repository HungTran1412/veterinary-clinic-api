using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class ServiceConstant
    {
        // Bạn cần đảm bảo VeterinaryClinicCacheConstants.SERVICE đã được định nghĩa
        // Nếu chưa, tôi sẽ dùng tạm chuỗi "Service"
        public const string CachePrefix = "Service"; 
        public const string SelectItemCacheSubfix = VeterinaryClinicCacheConstants.LIST_SELECT;
        
        public static string BuildCacheKey(string id = "")
        {
            if (string.IsNullOrEmpty(id))
            {
                //Cache cho danh sách combobox
                return $"{CachePrefix}-{SelectItemCacheSubfix}";
            }
            else
            {
                //Cache cho item
                return $"{CachePrefix}-{id}";
            }
        }
    }
}
