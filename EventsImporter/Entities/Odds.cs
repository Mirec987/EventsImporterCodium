namespace EventImporter.Entities
{
    public class Odds
    {
        public int ProviderOddsID { get; set; }
        public string OddsName { get; set; }
        public decimal OddsRate { get; set; }
        public string Status { get; set; }
        public long ProviderEventID { get; set; }
    }
}
