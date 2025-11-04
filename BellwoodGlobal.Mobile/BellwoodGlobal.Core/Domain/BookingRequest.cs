using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BellwoodGlobal.Core.Domain;

public sealed class BookingRequest
{
    public string? VehicleClass { get; set; }
    public QuoteDraft Draft { get; set; } = new();
}
