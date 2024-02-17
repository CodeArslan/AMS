using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace AMS.Models
{
    public class Shift
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Shift Type Name is required")]
        [DisplayName("Shift Type:")]
        public string shiftType { get; set; }
        [Required(ErrorMessage = "Start Time is required")]
        [DisplayName("Start Time:")]
        public TimeSpan startTime { get; set; }
        [Required(ErrorMessage = "End Time is required")]
        [DisplayName("End Time:")]
        public TimeSpan endTime { get; set; }
        [DisplayName("Duration:")]
        public TimeSpan duration => endTime - startTime;
        [DisplayName("Location:")]
        public string location { get; set; }
        [Required(ErrorMessage = "Client Name is required")]
        [DisplayName("Client:")]
        public string clientName { get; set; }
        public bool isActive { get; set; }
    }
}