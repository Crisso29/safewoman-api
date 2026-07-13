namespace SafeWoman.Domain.Exceptions;

public class DomainException : Exception
{
    /// Código estable de error para que el frontend pueda reaccionar por código
    /// (ej. abrir la pantalla de login) en vez de hacer string matching sobre el
    /// mensaje. Es opcional para no romper llamadas existentes.
    public string? Code { get; }

    public DomainException(string message) : base(message) { }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
