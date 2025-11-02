using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DelEdAllotment.Models
{
    public class Seat_Allotment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int registration_no { get; set; }
        public string name { get; set; }
        public string? center_id   { get; set; }
        public int room_number { get; set; }
        public int seat_row {  get; set; }
        public int seat_number { get; set; }
        public DateTime allotment_date { get; set; }
        public int city_code { get; set; }
        public int center_code { get; set; }
        public int? roll_no { get; set; }
    }
}
