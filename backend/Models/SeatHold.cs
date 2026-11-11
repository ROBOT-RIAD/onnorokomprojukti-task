using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAdmission.Api.Models;

public static class HoldStatus
{
    public const string Held = "HELD";
    public const string Confirmed = "CONFIRMED";
    public const string Expired = "EXPIRED";
    public const string Cancelled = "CANCELLED";
}

[Table("Holds")]
public class SeatHold
{
    [Key]
    [MaxLength(50)]
    public string HoldId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ClassId { get; set; } = string.Empty;

    [ForeignKey(nameof(ClassId))]
    public SchoolClass? Class { get; set; }

    [Required]
    [MaxLength(20)]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = HoldStatus.Held;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }
}
