using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AMS.Models
{
    public class Labour
    {
        public int Id { get; set; }
        [Display(Name = "First Name")]
        [Required]
        public string FirstName { get; set; }
        [Display(Name = "Last Name")]
        [Required]
        public string LastName { get; set; }
        [Display(Name = "Email")]
        [EmailAddress]
        [Required]

        public string Email { get; set; }
        [Required]

        public string CNIC { get; set; }
        [Required]
        [Display(Name = "Per Hour Wage")]

        public int perHour { get; set; }
        public int totalPay {  get; set; }

        public Department Department { get; set; }
        [ForeignKey("Department")]
        [Display(Name = "Department")]
        public int departmentId { get; set; }

        public Shift Shift { get; set; }
        [ForeignKey("Shift")]
        [Display(Name = "Shift")]
        public int? shiftId { get; set; }
    }
}