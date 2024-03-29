using ChargePlan.Domain.Exceptions;
using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Service.Facades;

public class UserPermissionsFacade
{
    private readonly IUserAuthorisationRepository _userAuthRepo;
    private readonly IUserIdAccessor _userId;

    public UserPermissionsFacade(IUserIdAccessor userId, IUserAuthorisationRepository userAuthRepo)
    {
        _userAuthRepo = userAuthRepo;
        _userId = userId;
    }

    public Guid Id
    {
        get
        {
            if (_userId.UserId == Guid.Empty) throw new NotPermittedException("Empty UserId");
            return _userId.UserId;
        }
    }

    public async Task<bool> IsAdministrator() => (await _userAuthRepo.GetAsync(Id))?.IsAdministrator == true;
}