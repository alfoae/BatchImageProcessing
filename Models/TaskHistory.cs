using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing.Models 
{ 
    public class TaskHistory 
    { 
        public string TaskName { get; set; } 
        public string Status { get; set; } 
        public DateTime Date { get; set; } 
        public int FileCount { get; set; } 
    } 
}
