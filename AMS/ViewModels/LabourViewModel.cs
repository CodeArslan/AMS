using AMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AMS.ViewModels
{
    public class LabourViewModel
    {
        public Labour Labour { get; set; }
        public List<Department> Department { get; set; }
        public List<Shift> Shift { get; set; }
    }
}