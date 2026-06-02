namespace VeterinaryClinic.Shared
{
    public class ClinicInfoSettings
    {
        public const string SECTION_NAME = "ClinicInfo";

        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
