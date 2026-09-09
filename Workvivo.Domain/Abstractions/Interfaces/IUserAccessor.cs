namespace Workvivo.Domain.Abstractions.Interfaces;
public interface IUserAccessor
{
    string? GetCurrentUserId();
    string? GetCurrentUserTypeCode();
    string? GetCurrentUserApplicationId();
}
