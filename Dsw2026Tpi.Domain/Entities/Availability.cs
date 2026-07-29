

namespace Dsw2026Tpi.Domain.Entities;
public class Availability : EntityBase
{
    public Guid DoctorId { get; init; }
    public DateTime Start { get; init; }

    #region Constructor for EF
#pragma warning disable CS8618
    private Availability() { }
#pragma warning restore CS8618
    #endregion

    public Availability(Guid doctorId, DateTime start, Guid? id = null) : base(id)
    {
        DoctorId = doctorId;
        Start = start;
    }
}
