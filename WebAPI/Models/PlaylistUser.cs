using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class PlaylistUser
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int? UserId { get; set; }

    public virtual User? User { get; set; }

    public virtual ICollection<Song> Songs { get; set; } = new List<Song>();
}
