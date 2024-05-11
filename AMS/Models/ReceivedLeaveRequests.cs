using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AMS.Models
{
    public class ReceivedLeaveRequests
    {
        public int Id {  get; set; }
        public string Name { get; set; }
        [DisplayName("Email")]
        public string From {  get; set; }
        [DisplayName("Reason")]

        public string Message {  get; set; }
        public string Subject {  get; set; }
        public DateTime? Date { get; set; } = DateTime.Now;
        public bool isRead {  get; set; }
        public string Decision { get; set; }
        [Required]
        public string Reason { get; set; }
        public ApplicationUser Labour { get; set; }
        [ForeignKey("Labour")]
        [Display(Name = "Labour")]
        public string labourId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        [ForeignKey("ApplicationUser")]
        [Display(Name = "Employee")]
        public string employeeId { get; set; }

    }
}