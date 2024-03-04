using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWorkflow.Maui.Models
{
    internal class MyTask
    {
        public Int32 Id { get; set; }
        public Int32 parentid { get; set; }
        public string Text { get; set; }
    }
}
