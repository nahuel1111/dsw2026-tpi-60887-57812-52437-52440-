using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignInService _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(UserManager<ApplicationUser> userManager,
        ISignInService signInManager,
        RoleManager<IdentityRole> roleManager,
        JwtService jwtService,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<LoginAdminModel.Response> LoginAdmin(LoginAdminModel.Request request)
    {
        if (!request.Email.IsEmailValid())
        {
            throw new ValidationException(
                "Email es inválido",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("email", "Formato inválido");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new ValidationException(
                "Password inválido",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("password", "Debe tener al menos 8 caracteres");
        }

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            throw new AuthenticationException(
       "Credenciales inválidas",
       ErrorCodes.AUTHENTICATION_FAILED)
       .WithDetail("login", "Usuario o contraseña incorrectos");
        }

        var passwordCorrect = await _userManager.CheckPasswordAsync(
            user,
            request.Password
        );

        if (!passwordCorrect)
        {
            _logger.LogError("Intento de login fallido para: {Email}", request.Email);
            throw new AuthenticationException(
          "Credenciales inválidas",
          ErrorCodes.AUTHENTICATION_FAILED)
          .WithDetail("login", "Usuario o contraseña incorrectos");
        }

        var roles = await _userManager.GetRolesAsync(user);

        var role = roles.FirstOrDefault();

        var token = _jwtService.GenerateToken(
            user.UserName!,
            role
        );

        return new LoginAdminModel.Response(token, role);
    }

    public async Task<LoginPatientModel.Response> LoginPatient(LoginPatientModel.Request request)
    {

        if (!request.Email.IsEmailValid())
        {
            throw new ValidationException(
                "Email es inválido",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("email", "Formato inválido");
        }

        if (request.Dni is null)
        {
            throw new ValidationException(
                "DNI obligatorio",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("dni", "Debe ingresar un DNI");
        }

        if (!request.Dni.IsDniValid())
        {
            throw new ValidationException(
                "DNI inválido",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("dni", "Debe contener entre 7 y 8 dígitos");
        }

        var dniStr = request.Dni.Value.ToString();

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                Dni = dniStr,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
            {
                throw new ConflictException(
                    "Creación fallida",
                    "Creación de usuario fallida")
                    .WithDetail(createResult.Errors.Select(e => (e.Code, e.Description)));
            }

            await _userManager.AddToRoleAsync(user, Roles.Patient);

            _logger.LogInformation("Paciente creado: {Email}", request.Email);
        }
        else if (user.Dni != dniStr)
        {
            _logger.LogWarning("DNI incorrecto para: {Email}", request.Email);
            throw new AuthenticationException("credenciales invalidadas", ErrorCodes.LOGIN_INVALID)
        .WithDetail("login", "Usuario o DNI incorrectos");
        }

        var roleUpper = Roles.Patient.ToUpperInvariant();

        var token = _jwtService.GenerateToken(
            user.UserName!,
            roleUpper);

        return new LoginPatientModel.Response(token, roleUpper);
    }
}


