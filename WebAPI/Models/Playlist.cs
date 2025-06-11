using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class Playlist
{
    public int PlaylistId { get; set; }

    public string? PlaylistName { get; set; }

    public string? PlaylistImage { get; set; }

    public virtual ICollection<Song> Songs { get; set; } = new List<Song>();
}
