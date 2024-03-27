using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AMS.Models
{
    public class LeaveResponse
    {
        public int Id {  get; set; }
        public string Decision {  get; set; }
        public string Message {  get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public ReceivedLeaveRequests ReceivedLeaveRequests { get; set; }
        [ForeignKey("ReceivedLeaveRequests")]
        public int rlrId { get; set; }


    }
}