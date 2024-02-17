using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace AMS.Models
{
    public class Department
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Department Name is required")]
        [DisplayName("Department Name:")]
        public string deptName { get; set; }

        public bool isActive { get; set; }
    }
}