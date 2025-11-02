using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DelEdAllotment.Models
{
    public class Room
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int? CityCode { get; set; }
        public string? CentreCode { get; set; }
        public int? RoomNo { get; set; }
        public int? RoomCapacity { get; set; }


    }
}
