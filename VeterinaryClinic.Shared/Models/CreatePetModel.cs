namespace VeterinaryClinic.Shared;

public class CreatePetModel
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Age { get; set; }
    public string OwnerName { get; set; } = string.Empty;
}