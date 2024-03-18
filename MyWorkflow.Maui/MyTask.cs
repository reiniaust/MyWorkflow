using System;
using System.Collections.Generic;
using System.Globalization;
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
        public string due_on { get; set; }
        public bool completed { get; set; }
        public string StatusPlusName
        {
            get { 
                return completed == true ? "Erledigt: " + name : name; 
            }
        }

        public string ViewDate
        {
            get
            {
                string returnDate = "";
                if (!string.IsNullOrEmpty(created_at))
                {
                    if (created_at.Contains("-"))
                    {
                        DateTime utcDateTime = DateTime.Parse(created_at, null, DateTimeStyles.RoundtripKind); // Parse with UTC kind
                        DateTime convertedDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
                        returnDate = convertedDateTime.ToString();
                    } else
                    {
                        returnDate = created_at;
                    }
                    returnDate = returnDate.Substring(0, 16);
                } 
                return returnDate;
            }
        }


        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
