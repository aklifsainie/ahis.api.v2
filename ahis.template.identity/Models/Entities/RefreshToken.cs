using System;
using System.ComponentModel.DataAnnotations;

namespace ahis.template.identity.Models.Entities
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public int SessionId { get; set; }
        public int? ParentTokenId { get; set; }
        public byte[] TokenHash { get; set; } = null!;
        public string? SecurityVersion { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
