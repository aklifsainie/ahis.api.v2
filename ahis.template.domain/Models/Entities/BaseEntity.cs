using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ahis.template.domain.Models.Entities
{
    public class BaseEntity
    {
        [Required]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public bool IsActive { get; set; } = true;
        [Required]
        public bool IsDelete { get; set; } = false;
        public string? RegisterBy { get; set; } = null;
        [Required]
        public DateTime RegisterDate { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; } = null;
        public DateTime? UpdatedDate { get; set; } = null;
        public string? Remarks { get; set; } = null;
    }
}
