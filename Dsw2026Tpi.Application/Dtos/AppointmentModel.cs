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


    }
}
