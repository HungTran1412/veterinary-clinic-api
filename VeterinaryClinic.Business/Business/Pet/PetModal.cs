namespace VeterinaryClinic.Business
{
    public class PetBaseModal
    {
        public int Id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
    }

    public class PetModal : PetBaseModal
    {
        
    }

    public class CreatePetModal : PetModal
    {
        private int CreatedUser { get; set; }
    }
}
