namespace EventImporter.Entities
{
    public class EventMessage
    {
        public Guid MessageID { get; set; }
        public SportsEvent Event { get; set; }
    }
}
