using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public enum AppointmentState
    {
        BOOKED = 0,
        CANCELLED = 1,
        ATTENDED = 2, 
        NO_SHOW  = 3,
    }
}
