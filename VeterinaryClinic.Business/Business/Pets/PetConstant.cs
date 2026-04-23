namespace VeterinaryClinic.Business
{
    public static class PetConstant
    {
        private const string PetCacheKey = "pets";

        public static string BuildCacheKey(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? PetCacheKey : $"{PetCacheKey}-{id}";
        }
    }
}
