using System;
using System.Collections.Generic;
using TimeTrackerApp.Models;

namespace TimeTrackerApp.Models.ViewModels
{
    public class CalendarViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime SelectedMonth { get; set; }
        public List<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
        public List<DateTime> DaysInCalendar { get; set; } = new List<DateTime>();
        
        public string MonthName => SelectedMonth.ToString("MMMM yyyy");
        
        public DateTime PrevMonth => SelectedMonth.AddMonths(-1);
        public DateTime NextMonth => SelectedMonth.AddMonths(1);
    }
}
