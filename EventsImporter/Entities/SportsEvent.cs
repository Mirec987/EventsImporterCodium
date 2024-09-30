namespace EventImporter.Entities
{
    public class SportsEvent
    {
        public int ProviderEventID { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public List<Odds> OddsList { get; set; }
    }
}
