using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;

namespace MyWorkflow
{
    public class MyTask : ICloneable
    {
        public string gid { get; set; }
        public string parentid { get; set; }
        public string name { get; set; }
        public string notes { get; set; }
        // Add additional properties as needed
        public string created_at { get; set; }
        public string modified_at { get; set; }
        public string due_on { get; set; }
        public string next_due_on { get; set; }
        public Assignee assignee { get; set; }
        public bool completed { get; set; }
        public List<string> dependencies { get; set; } = new List<string>(); 
        public string NameInList { get; set; }

        public Color DueDateColor
        {
            get {
                Color color = Colors.White;
                if (completed)
                {  
                    color = Colors.Black; 
                }
                else
                {
                    if (next_due_on != null && !completed)
                    {
                        DateTime dueDate = DateTime.Parse(next_due_on);
                            if (dueDate < DateTime.Today)
                                color = Colors.Red;
                            else
                                if (dueDate.Date == DateTime.Today)
                                    color = Colors.Orange;
                                else
                                    if (dueDate.Date == DateTime.Today.AddDays(1))
                                        color = Colors.Yellow;
                                    else
                                        if (dueDate.Date == DateTime.Today.AddDays(2))
                                            color = Colors.Green;
                                        else
                                            color = Colors.Blue;
                    }
                }
                return color;
            }
        }

        public string OrderDate
        {
            get
            {
                if (completed)
                    return modified_at;
                else
                    if (next_due_on != null)
                        return next_due_on;
                    else
                        if (modified_at == null)
                            return created_at;
                        else
                            return modified_at;
            }
        }
        public string ViewDate
        {
            get
            {
                if (completed)
                {
                    return "Erledigt: " + LocalDateString(modified_at);
                }
                else
                {
                    if (next_due_on != null)
                    {
                        return "Termin: " + LocalDateString(next_due_on) + " (" + LocalDateString(modified_at == null ? created_at : modified_at) + ")";
                    }
                    else
                    {
                        if (modified_at == null)
                        {
                            return LocalDateString(created_at);
                        }
                        else
                        {
                            return "Geändert: " + LocalDateString(modified_at);
                        }
                    }
                }
            }
        }

        public string LocalDateString(string dateString)
        {
            string returnDate = "";
            if (!string.IsNullOrEmpty(dateString))
            {
                if (dateString.Contains("-"))
                {
                    DateTime utcDateTime = DateTime.Parse(dateString, null, DateTimeStyles.RoundtripKind); // Parse with UTC kind
                    DateTime convertedDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
                    returnDate = convertedDateTime.ToString();
                }
                else
                {
                    returnDate = dateString;
                }
                returnDate = returnDate.Substring(0, 16);
            }
            return returnDate;
        }


        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
