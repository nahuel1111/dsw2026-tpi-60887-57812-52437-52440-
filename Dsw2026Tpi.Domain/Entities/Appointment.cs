using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Appointment : EntityBase
    {
        public Guid DoctorId { get; init; }

        public Guid AvailabilityId { get; init; }

        public long PatientDni { get; init; }

        public string Reason { get; private set; }

        public AppointmentState State { get; private set; }

        #region Constructor for EF
#pragma warning disable CS8618
        private Appointment() { }
#pragma warning restore CS8618
        #endregion

        public Appointment (Guid doctorid, Guid availabilityid, long patientdni, string reason, AppointmentState state = AppointmentState.BOOKED, Guid? id = null): base(id) {

            DoctorId = doctorid;
            AvailabilityId = availabilityid;
            PatientDni = patientdni;
            Reason = reason;
            State = state;

        }
        public void Cancel() {
        
            State = AppointmentState.CANCELLED;
        
     
        }
        public void Attend()
        {
            State = AppointmentState.ATTENDED;

        }
        public void NoShow()
        {

            State = AppointmentState.NO_SHOW;

        }
}
}