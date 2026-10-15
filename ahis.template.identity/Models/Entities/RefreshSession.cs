using System;
using System.ComponentModel.DataAnnotations;

namespace ahis.template.identity.Models.Entities;

public class RefreshSession
{
    [Key]
    public int Id { get; set; }

    public Guid PublicId { get; set; }
    public string UserId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
}
