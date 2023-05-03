public class NotPermittedException : Exception
{
    public NotPermittedException() : base("You don't have permissions to do this") { }

    public NotPermittedException(string message) : base(message) { }
}