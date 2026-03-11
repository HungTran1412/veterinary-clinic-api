using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class UserConstant
    {
        public const string CachePrefix = VeterinaryClinicCacheConstants.USER;
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

