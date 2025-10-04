using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DelEdAllotment.Models
{
    public class Registrations
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int RegistrationNo { get; set; }
        public string Warg { get; set; }
        public string Name { get; set; }
        public string? FName { get; set; }
        public string? MName { get; set; }
        public string? HusbandName { get; set; }
        public string Email { get; set; }
        public string MobileNo { get; set; }
        public string Gender { get; set; }
        public DateTime DOB { get; set; }
        public string Category { get; set; }
        public string Ph {  get; set; }
        public int PreferredCityCode { get; set; }
        public int Centre2 { get; set; }
        public string Graduation {  get; set; }
        public DateTime GraduationDate { get; set; }
        public string PhotoId { get; set; }

        public string? SubCategory { get; set; }
        public string HomeDistrict { get; set; }
        public string? PhType { get; set; }
        public int? FeeAmount { get; set; }
        public long? TransactionId { get; set; }
        public string? Remarks { get; set; }
        public DateTime? RetirementDate { get; set; }
        public string? Address { get; set; }
        public int? AssignedCentre { get; set; }
        public int? RollNumber { get; set; }
        public string? ImagePath {  get; set; }
        public string? SignaturePath { get; set; }

    }
}
