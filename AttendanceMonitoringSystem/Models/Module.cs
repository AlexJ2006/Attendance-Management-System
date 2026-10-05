namespace AttendanceMonitoringSystem.Models
{
    public class Module
    {
        public int ModuleId { get; set; }

        public string ModuleCode { get; set; }

        public string ModuleName { get; set; }

        public string Course { get; set; }

        public int YearOfStudy { get; set; }

        public string ModuleLeader { get; set; }
    }
}