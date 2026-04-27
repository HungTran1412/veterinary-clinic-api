namespace VeterinaryClinic.Shared
{
    /// <summary>
    /// Cachprefix - Lưu ý không được viết hoa vì elastic không nhận chữ hoa trong index
    /// </summary>
    public class VeterinaryClinicCacheConstants
    {
        #region Cache prefix

        public const string LIST_SELECT = "list-select";
        public const string PERMISSION = "permission";
        public const string SPECIALIZATION = "specialization";
        public const string AUTHORIZATION = "authorization";
        public const string USER = "users";
        public const string SERVICE = "service";
        public const string EMAIL_LOGS = "email-logs";
        public const string PHOTO_UPLOAD = "photo-upload";
        public const string WORK_SCHEDULE = "work-schedule";
        public const string PETS = "pets";
        public const string DOCTOR_SPECIALIZATION = "doctor-specializations";
        public const string APPOINMENT = "appointments";
        public const string MEDICAL_RECORD = "medical-records";

        #endregion
    }   

    #region System

    public record SelectItemModel
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
    
    public class ClaimConstants
    {
        // Standard OpenID Connect claims
        public const string PREFERRED_USERNAME = "preferred_username";

        // Custom claims cho hệ thống
        public const string AVATAR = "x-avatar";
        public const string APP_ID = "x-app-id";
        public const string ORG_ID = "x-org-id";
        public const string ROLES = "x-role";
        public const string RIGHTS = "x-right";
        public const string PERMISSIONS = "x-permission";
        public const string ISSUED_AT = "x-iat";
        public const string EXPIRES_AT = "x-exp";
        public const string CHANNEL = "x-channel";
        public const string REQUEST_ID = "x-request-id";
        public const string API_KEY = "x-api-key";
        public const string IS_STUDENT = "is_student";
        public const string AUTHENTICATION_METHOD = "x-authentication-method";
    }

    
    public class QueryFilter
    {
        public const int DefaultPageNumber = 1;
        public const int DefaultPageSize = 20;
    }

    #endregion
    
}