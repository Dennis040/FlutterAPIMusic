using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class HoaDonAdmin
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public DateOnly? Date { get; set; }

    public double? Total { get; set; }

    public DateOnly? EndDate { get; set; }

    public virtual User? User { get; set; }
}
