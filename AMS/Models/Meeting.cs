using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace AMS.Models
{
    public class Meeting
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please provide meeting date")]
        [Display(Name = "Date")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Please provide meeting start time")]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Please provide meeting end time")]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Please provide meeting agenda")]
        public string Agenda { get; set; }

        [Required(ErrorMessage = "Please provide meeting location")]
        public string Location { get; set; }
        public string Status { get; set; }
    }
}