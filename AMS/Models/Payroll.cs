using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AMS.Models
{
    public class Payroll
    {
        public int Id { get; set; }
        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public double TotalHoursWorked { get; set; }

        [Required]
        public decimal TotalSalary { get; set; } = 0;

        public decimal salaryApproved { get; set; } = 0;
        public decimal requestedSalary { get; set; } = 0;
        public decimal? Bonus { get; set; }
        public bool isApproved {  get; set; }
        public bool isSendForApproval {  get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        [ForeignKey("ApplicationUser")]
        [Display(Name = "Employee")]
        public string employeeId { get; set; }
        public Labour Labour { get; set; }
        [ForeignKey("Labour")]
        [Display(Name = "Labour")]
        public int? labourId { get; set; }
    }
}