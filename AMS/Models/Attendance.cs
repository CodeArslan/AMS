using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public DateTime date { get; set; }
        public TimeSpan timeIn { get; set; }
        public TimeSpan timeOut { get; set; }
        public Labour Labour { get; set; }
        [ForeignKey("Labour")]
        [Display(Name = "Labour")]
        public int? labourId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        [ForeignKey("ApplicationUser")]
        [Display(Name = "Employee")]
        public string employeeId { get; set; }

        public string totalWorkedTime { get; set; }
    }
}