using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BellwoodGlobal.Core.Domain;

public sealed class BookingDetail
{
    public string Id { get; set; } = default!;
    public DateTime CreatedUtc { get; set; }
    public BookingStatus Status { get; set; }
    public string? VehicleClass { get; set; }
    public QuoteDraft Draft { get; set; } = new();
}
