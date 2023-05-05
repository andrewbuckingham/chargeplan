using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Api.Auth;

public class UserIdAccessor : IUserIdAccessor
{
    public Guid UserId { get; set; }
}