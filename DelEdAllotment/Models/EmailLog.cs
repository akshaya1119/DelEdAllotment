using System;
using System.ComponentModel.DataAnnotations;

namespace DelEdAllotment.Models
{
    public class EmailLog
    {
        [Key]
        public int Id { get; set; }

        public string Session { get; set; }
        public int LastSentRegNo { get; set; }
        public int TotalSent { get; set; }
        public DateTime LastSentAt { get; set; }
        public string Status { get; set; } // e.g., "InProgress", "Completed"
    }
}
