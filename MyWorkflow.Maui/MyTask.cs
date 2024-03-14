using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWorkflow.Maui
{
    public class MyTask : ICloneable
    {
        public string gid { get; set; }
        public string parentid { get; set; }
        public string name { get; set; }
        // Add additional properties as needed
        public string created_at { get; set; }
        public bool completed { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
