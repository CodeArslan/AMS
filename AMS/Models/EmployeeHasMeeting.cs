using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AMS.Models
{
    public class EmployeeHasMeeting
    {
        public int Id { get; set; }
        public Meeting Meeting { get; set; }
        [ForeignKey("Meeting")]
        [Display(Name = "Meeting")]
        public int meetingId { get; set; }

        public ApplicationUser ApplicationUser { get; set; }
        [ForeignKey("ApplicationUser")]
        [Display(Name = "Employee")]
        public string employeeId { get; set; }
    }
}