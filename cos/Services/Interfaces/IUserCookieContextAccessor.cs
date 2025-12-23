using cos.Models;

namespace cos.Interfaces;

public interface IUserCookieContextAccessor
{
    UserCookieContext Get();
}