using AMS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace AMS.ViewModels
{
    public class EmployeeMeetingViewModel
    {
        public List<Department> Departments { get; set; }
        public Meeting Meeting { get; set; }
        public EmployeeHasMeeting EmployeeHasMeeting { get; set; }
        public List<ApplicationUser> User { get; set; }

        [NotMapped]
        public string EmployeeIds { get; set; }
    }
}