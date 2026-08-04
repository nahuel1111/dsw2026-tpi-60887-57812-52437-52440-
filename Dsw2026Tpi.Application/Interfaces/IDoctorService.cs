using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IDoctorService
{
    Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null);

    Task<DoctorModel.Response> CreateAsync(DoctorModel.Request request);
    Task<DoctorModel.Response> UpdateAsync(Guid id, DoctorModel.Request request);
    Task DeleteAsync(Guid id);

    Task<IEnumerable<DoctorModel.AvailabilityResponse>> GetAvailabilitiesAsync(Guid id);
}








