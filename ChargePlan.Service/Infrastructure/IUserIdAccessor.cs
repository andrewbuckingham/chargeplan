namespace ChargePlan.Service.Infrastructure;

public interface IUserIdAccessor
{
    public Guid UserId { get; }
}