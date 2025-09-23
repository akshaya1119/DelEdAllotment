﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DelEdAllotment.Models
{
    public class Centres
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int CityCode { get; set; }
        public string CityName { get; set; }
        public int CentreCode { get; set; }
        public string CentreName { get; set;}
        public int Capacity { get; set; }
        public int? IncreasedCapacity { get; set; }
    }
}
