using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Data.Options;
using System.Text.Json;



namespace Dsw2026Tpi.Api.Services;

public class HolidayService : IHolidayService
{
    private readonly HashSet<(int Month, int Day)> _nonWorkingDays = new();

    public HolidayService(IHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "nonworkingdays.json");
        if (File.Exists(path))
        {
            try
            {
                var text = File.ReadAllText(path);
                var byMonth = JsonSerializer.Deserialize<Dictionary<string, List<int>>>(text, JsonOptions.JsonSerializerOptions);
                if (byMonth != null)
                {
                    foreach (var kv in byMonth)
                    {
                        if (int.TryParse(kv.Key, out var month))
                        {
                            foreach (var day in kv.Value)
                            {
                                _nonWorkingDays.Add((month, day));
                            }
                        }
                    }
                }
                else
                {
                    var dates = JsonSerializer.Deserialize<List<string>>(text, JsonOptions.JsonSerializerOptions);
                    if (dates != null)
                    {
                        foreach (var s in dates)
                        {
                            if (DateOnly.TryParse(s, out var dt))
                            {
                                _nonWorkingDays.Add((dt.Month, dt.Day));
                                continue;
                            }

                            var parts = s.Split(new[] { '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                if (parts[0].Length == 4 && parts.Length >= 3)
                                {
                                    if (int.TryParse(parts[1], out var mm) && int.TryParse(parts[2], out var dd))
                                    {
                                        _nonWorkingDays.Add((mm, dd));
                                    }
                                }
                                else
                                {
                                    if (int.TryParse(parts[0], out var m) && int.TryParse(parts[1], out var d))
                                    {
                                        _nonWorkingDays.Add((m, d));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }

    public Task<bool> IsNonWorkingDayAsync(DateTime date)
    {
        return Task.FromResult(_nonWorkingDays.Contains((date.Month, date.Day)));
    }
}
