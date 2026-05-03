using System;
using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public abstract record PetBaseModel
    {
        public int Id { get; init; }
        public string? Code { get; init; }
        [Required(ErrorMessage = "pet.name.required")]
        public string Name { get; init; }
        [Required(ErrorMessage = "pet.species.required")]
        public string Species { get; init; }
        [Required(ErrorMessage = "pet.breed.required")]
        public string Breed { get; init; }
        [Required(ErrorMessage = "pet.gender.required")]
        public bool Gender { get; init; }
        public bool IsNeutered { get; init; } = false;
        [Required(ErrorMessage = "pet.birth_date.required")]
        public DateTime BirthDate { get; init; }
        public double? Weight { get; init; }
        public string? Color { get; init; }
        public string? ImageUrl { get; init; }
        public string? Note { get; init; }
        public bool IsActive { get; init; } = true;
        public int Order { get; init; }
        public DateTime? CreatedDate { get; init; }
    }

    public record PetModel : PetBaseModel
    {
        [Required(ErrorMessage = "pet.owner_id.required")]
        public int OwnerId { get; init; }
        public string OwnerName { get; init; }
    }

    public record CreatePetModel
    {
        [Required(ErrorMessage = "pet.name.required")]
        public string Name { get; init; }
        [Required(ErrorMessage = "pet.species.required")]
        public string Species { get; init; }
        [Required(ErrorMessage = "pet.breed.required")]
        public string Breed { get; init; }
        [Required(ErrorMessage = "pet.gender.required")]
        public bool Gender { get; init; }
        public bool IsNeutered { get; init; } = false;
        [Required(ErrorMessage = "pet.birth_date.required")]
        public DateTime BirthDate { get; init; }
        public double? Weight { get; init; }
        public string? Color { get; init; }
        public string? ImageUrl { get; init; }
        public int? OwnerId { get; init; } // Nullable, not required
        public string? Note { get; init; }
    }

    public record UpdatePetModel : PetBaseModel
    {
        public int? ModifiedUserId { get; init; }

        public void UpdateEntity(VcPets entity)
        {
            entity.Name = this.Name;
            entity.Species = this.Species;
            entity.Breed = this.Breed;
            entity.Gender = this.Gender;
            entity.IsNeutered = this.IsNeutered;
            entity.BirthDate = this.BirthDate;
            entity.Weight = this.Weight;
            entity.Color = this.Color ?? string.Empty;
            entity.ImageUrl = this.ImageUrl ?? string.Empty;
            entity.Note = this.Note ?? string.Empty;
            entity.Order = this.Order;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public record PetSelectItemModel : SelectItemModel
    {
    }

    public record PetFilterModel : BaseQueryFilterModel
    {
        public int? OwnerId { get; set; }
    }
}
