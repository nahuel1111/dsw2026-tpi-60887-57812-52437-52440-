using System.ComponentModel.DataAnnotations;

namespace Dsw2026Tpi.Application.Dtos;

public record SpecialityModel
{
    public record Request
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; init; }

        [Required]
        [StringLength(100, MinimumLength = 10)]
        public string Description { get; init; }
    }

    public record Response(Guid Id, string Name, string Description);
}