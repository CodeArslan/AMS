using AMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AMS.ViewModels
{
    public class LabourShiftViewModel
    {
        public Labour Labours { get; set; }
        public List<Shift> Shifts { get; set; }
    }
}