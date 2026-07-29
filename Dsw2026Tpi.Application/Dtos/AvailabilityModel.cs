namespace Dsw2026Tpi.Application.Dtos;

public record AvailabilityModel
{
    public record DayRange(string Day, string StartTime, string EndTime);

    public record Request(Guid DoctorId, IEnumerable<DayRange> Days);

    public record Response(string Day, string StartTime, string EndTime);
}
