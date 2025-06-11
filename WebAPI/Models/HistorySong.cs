using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class HistorySong
{
    public int UserId { get; set; }

    public int SongId { get; set; }

    public int? OrderIndex { get; set; }

    public virtual Song Song { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
