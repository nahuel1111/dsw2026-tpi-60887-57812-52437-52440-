using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Interfaces
{
    public interface IAppointmentService
    {
        Task<AppointmentModel.Response> CreateAsync(AppointmentModel.Request request);
        Task<IEnumerable<AppointmentModel.Response>> GetByPatientAsync(long dni);
        Task CancelAsync(Guid id);
        Task<IEnumerable<AppointmentModel.SearchResult>> GetByDateAsync(DateTime date);
        Task<Pagination<AppointmentModel.SearchResult>> SearchAsync(Guid? specialtyId, Guid? doctorId, long? dni, DateTime? date, int pageSize, int pageIndex);
    }
}
