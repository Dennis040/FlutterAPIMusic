using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class Artist
{
    public int ArtistId { get; set; }

    public string? ArtistName { get; set; }

    public string? ArtistImage { get; set; }

    public virtual ICollection<Album> Albums { get; set; } = new List<Album>();

    public virtual ICollection<Song> Songs { get; set; } = new List<Song>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
