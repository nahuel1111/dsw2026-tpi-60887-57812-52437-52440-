using System;
using System.Threading.Tasks;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IHolidayService
{
    Task<bool> IsNonWorkingDayAsync(DateTime date);
}
