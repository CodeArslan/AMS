using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    }
}