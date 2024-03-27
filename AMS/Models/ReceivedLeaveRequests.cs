using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AMS.Models
{
    public class ReceivedLeaveRequests
    {
        public int Id {  get; set; }
        public string Name { get; set; }
        public string From {  get; set; }
        public string Message {  get; set; }
        public string Subject {  get; set; }
        public DateTime? Date { get; set; } = DateTime.Now;
        public bool isRead {  get; set; }
        public string Decision { get; set; }

    }
}