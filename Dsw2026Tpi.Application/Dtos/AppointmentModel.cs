using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos
{
    public static class AppointmentModel
    {

        public record PatientDto(long dni);
        public record Request(Guid DoctorId, Guid AvailabilityId, PatientDto Patient, string Reason);

        public record Response(Guid Id, Guid DoctorId, Guid AvailabilityId, long PatientDni, string Reason, string State, DateTime CreatedAt, DateTime UpdatedAt);

        public record SearchResult(string Specialty, string Doctor, DateTime AvailableTime, Guid DoctorId, Guid AvailabilityId);

        // New search response item matching required structure
        public record SearchItem(
            Guid appointmentsId,
            string appointmentsStatus,
            PatientInfo patient,
            DoctorInfo doctor);

        public record PatientInfo(long dni, string? fullName);

        public record DoctorInfo(Guid doctorId, string name, SpecialtyInfo specialty);

        public record SpecialtyInfo(Guid specialtyId, string name);

        public record SearchResponse(int pageSize, int pageIndex, IEnumerable<SearchItem> data, int total);


    }
}
