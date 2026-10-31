namespace Controlwork_1.Exeptions;

public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}