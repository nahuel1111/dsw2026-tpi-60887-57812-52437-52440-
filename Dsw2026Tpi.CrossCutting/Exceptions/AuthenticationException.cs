using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

/// <summary>
/// Excepción que se lanza cuando falla la autenticación del usuario.
/// </summary>
public class AuthenticationException : AppException
{
    public AuthenticationException()
        : base(
            ErrorCodes.AUTHENTICATION_FAILED,
            nameof(ErrorCodes.AUTHENTICATION_FAILED))
    {
    }

    public AuthenticationException(string message, string errorCode) : base(message, errorCode)
    {
    }
}
