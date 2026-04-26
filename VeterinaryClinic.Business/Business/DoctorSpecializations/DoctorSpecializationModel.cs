using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Business
{
    public record DoctorSpecializationModel
    {
        [Required(ErrorMessage = "doctor_specialization.doctor_id.required")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "doctor_specialization.specialization_ids.required")]
        [MinLength(1, ErrorMessage = "doctor_specialization.specialization_ids.min_length")]
        public List<int> SpecializationIds { get; set; }
    }
}
