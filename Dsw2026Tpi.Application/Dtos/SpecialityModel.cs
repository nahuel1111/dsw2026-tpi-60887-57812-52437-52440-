using System.ComponentModel.DataAnnotations;

namespace Dsw2026Tpi.Application.Dtos;

public record SpecialityModel
{
    public record Request
    {
        public required string ?Name { get; init; }
        public required string ?Description { get; init; }
    }

    public record Response(Guid Id, string Name, string Description);
}