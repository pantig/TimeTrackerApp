namespace TimeTrackerApp.Models.ViewModels
{
    public class TimeGridEntry
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string? Description { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}