using AMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AMS.ViewModels
{
    public class LeaveResponseViewModel
    {
        public ReceivedLeaveRequests ReceivedLeaveRequests { get; set; }
        public LeaveResponse LeaveResponse { get; set; }
    }
}