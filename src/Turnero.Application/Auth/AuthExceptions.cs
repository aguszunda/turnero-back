namespace Turnero.Application.Auth;

public sealed class EmailAlreadyRegisteredException : Exception
{
    public EmailAlreadyRegisteredException() : base("El email ya esta registrado.")
    {
    }
}

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Email o contrasena incorrectos.")
    {
    }
}