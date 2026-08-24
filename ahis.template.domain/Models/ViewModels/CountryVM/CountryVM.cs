namespace ahis.template.domain.Models.ViewModels.CountryVM
{
    public class CountryVM
    {
        public int CountryId { get; set; }
        public string CountryFullname { get; set; }
        public string CountryShortname { get; set; }
        public string CountryDescription { get; set; }
        public string CountryCode2 { get; set; }
        public string CountryCode3 { get; set; }
        public string CountryIsoCode { get; set; }
        public bool IsActive { get; set; }
    }
}
