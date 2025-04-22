namespace cvBotConsole
{
    public class ResumeData
    {
        public int Step { get; set; } = 0;

        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Company { get; set; }

        public string? StartDate { get; set; }
        public string? EndDate { get; set; }

        public DateTime? StartDateTime { get; set; } // Для проверки дат
        public DateTime? EndDateTime { get; set; }   // Для проверки дат

        public string? Position { get; set; }
        public string? Experience { get; set; }
        public string? About { get; set; }
        public string? ImprovedAbout { get; set; }
    }
}
