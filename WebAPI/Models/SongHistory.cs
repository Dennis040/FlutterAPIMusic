using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class SongHistory
{
    public int HistoryId { get; set; }

    public int? UserId { get; set; }

    public int? SongId { get; set; }

    public DateTime? PlayTime { get; set; }

    public virtual Song? Song { get; set; }

    public virtual User? User { get; set; }
}
