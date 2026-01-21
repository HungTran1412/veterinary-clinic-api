using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Data;

public class Pet
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Age { get; set; }
    public string OwnerName { get; set; } = string.Empty;
}