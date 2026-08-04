using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Data.Options;
using Serilog;
using System.Text.Json;



namespace Dsw2026Tpi.Api.Services;

public class HolidayService : IHolidayService
{
    private readonly HashSet<(int Month, int Day)> _nonWorkingDays = new();

    public HolidayService(IHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "nonworkingdays.json");

        if (!File.Exists(path))
            return;

        try
        {
            var text = File.ReadAllText(path);

            var byMonth = JsonSerializer.Deserialize<Dictionary<string, List<int>>>(
                text,
                JsonOptions.JsonSerializerOptions);

            if (byMonth == null)
                return;

            foreach (var kv in byMonth)
            {
                if (!int.TryParse(kv.Key, out var month))
                    continue;

                foreach (var day in kv.Value)
                {
                    _nonWorkingDays.Add((month, day));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cargando feriados: {ex.Message}");
        }
    }


    public Task<bool> IsNonWorkingDayAsync(DateTime date)
    {
        return Task.FromResult(
            _nonWorkingDays.Contains((date.Month, date.Day))
        );
    }
}