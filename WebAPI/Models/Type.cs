using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class Type
{
    public int TypeId { get; set; }

    public string? NameType { get; set; }

    public virtual ICollection<Song> Songs { get; set; } = new List<Song>();
}
