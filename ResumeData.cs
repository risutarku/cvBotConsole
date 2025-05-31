namespace cvBotConsole
{
    public class ResumeData
    {
        public int Step { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }

        public List<WorkPlace> WorkPlaces { get; set; } = new();

        // Временные поля для ввода одного опыта
        public string Company { get; set; }
        public string StartDate { get; set; }
        public DateTime StartDateTime { get; set; }
        public string EndDate { get; set; }
        public string Position { get; set; }
        public string Experience { get; set; }

        public string About { get; set; }
        public string ImprovedAbout { get; set; }
    }
}
