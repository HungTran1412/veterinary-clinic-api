namespace VeterinaryClinic.Shared
{
    /// <summary>
    /// Cachprefix - Lưu ý không được viết hoa vì elastic không nhận chữ hoa trong index
    /// </summary>
    public class VeterinaryClinicCacheConstants
    {
        public const string LIST_SELECT = "list-select";
        public const string SPECIALIZATION = "specialization";
        public const string SERVICE = "service";
        public const string PET = "pettest";
    }   

    #region System

    public class SelectItemModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Note { get; set; } = "";
    }
    
    public class LanguageConstant
    {
        public const string VI = "vi-VN";
        public const string EN = "en-US";
    }
    
    public class QueryFilter
    {
        public const int DefaultPageNumber = 1;
        public const int DefaultPageSize = 20;
    }

    #endregion
    
}