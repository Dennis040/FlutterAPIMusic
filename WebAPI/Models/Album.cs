using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class Album
{
    public int AlbumId { get; set; }

    public string? AlbumName { get; set; }

    public string? AlbumImage { get; set; }

    public int? ArtistId { get; set; }

    public virtual Artist? Artist { get; set; }

    public virtual ICollection<Song> Songs { get; set; } = new List<Song>();
}
