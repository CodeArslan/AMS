using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.ComponentModel;

namespace AMS.Models
{
    public class LeaveResponse
    {
        public int Id {  get; set; }
        public string Decision {  get; set; }
        public string Message {  get; set; }
        [DisplayName("From Date")]

        public DateTime? From { get; set; }
        [DisplayName("To Date")]

        public DateTime? To { get; set; }
        public ReceivedLeaveRequests ReceivedLeaveRequests { get; set; }
        [ForeignKey("ReceivedLeaveRequests")]
        public int rlrId { get; set; }


    }
}