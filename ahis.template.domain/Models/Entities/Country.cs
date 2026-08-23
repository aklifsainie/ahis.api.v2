using ahis.template.domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ahis.template.domain.Models.Entities
{
    public class Country : BaseEntity, IAuditableEntity
    {
        [Required]
        public string CountryFullname { get; set; }
        [Required]
        public string CountryShortname { get; set; }
        public string? CountryDescription { get; set; } = null;
        [Required]
        public string CountryCode2 { get; set; }
        [Required]
        public string CountryCode3 { get; set; }
        [Required]
        public string CountryIsoCode { get; set; }
    }
}
