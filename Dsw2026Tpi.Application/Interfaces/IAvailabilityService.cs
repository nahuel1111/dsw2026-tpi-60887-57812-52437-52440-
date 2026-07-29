using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAvailabilityService
{
    Task<IEnumerable<AvailabilityModel.Response>> GetByDoctorAsync(Guid doctorId);
    Task CreateMonthAsync(AvailabilityModel.Request request);
    Task UpdateMonthAsync(AvailabilityModel.Request request);
}
