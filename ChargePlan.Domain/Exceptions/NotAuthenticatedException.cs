namespace ChargePlan.Domain.Exceptions;

public class NotAuthenticatedException : Exception
{
    public NotAuthenticatedException() : base("You must provide authentication to use this endpoint") { }

    public NotAuthenticatedException(string message) : base(message) { }
}