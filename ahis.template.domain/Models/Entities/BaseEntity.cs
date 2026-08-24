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
        [Column(TypeName = "datetime")]
        public DateTime RegisterDate { get; set; } = DateTime.UtcNow;

        public string? UpdatedBy { get; set; } = null;

        [Column(TypeName = "datetime")]
        public DateTime? UpdatedDate { get; set; } = null;


        [MaxLength(500)]
        public string? Remarks { get; set; } = null;
    }
}
