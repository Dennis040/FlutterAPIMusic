using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class User
{
    public int UserId { get; set; }

    public string? Username { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Role { get; set; }

    public string? Phone { get; set; }

    public string? Status { get; set; }

    public string? DeviceId { get; set; }


    // Email verification fields
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationCode { get; set; }
    public DateTime? EmailVerificationExpiry { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }

    // Timestamps
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<HistorySong> HistorySongs { get; set; } = new List<HistorySong>();

    public virtual ICollection<HoaDonAdmin> HoaDonAdmins { get; set; } = new List<HoaDonAdmin>();

    public virtual ICollection<HoaDonArtist> HoaDonArtists { get; set; } = new List<HoaDonArtist>();

    public virtual ICollection<PlaylistUser> PlaylistUsers { get; set; } = new List<PlaylistUser>();

    public virtual ICollection<SongHistory> SongHistories { get; set; } = new List<SongHistory>();

    public virtual ICollection<Artist> Artists { get; set; } = new List<Artist>();

    public virtual ICollection<Song> Songs { get; set; } = new List<Song>();
}
