using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using cos.Models;
using cos.Interfaces;

namespace cos.Services;

public class UserCookieContextAccessor : IUserCookieContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDataProtector _protector;

    public UserCookieContextAccessor(
        IHttpContextAccessor httpContextAccessor,
        IDataProtectionProvider provider)
    {
        _httpContextAccessor = httpContextAccessor;
        _protector = provider.CreateProtector("DataProtector");
    }

    public UserCookieContext Get()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return new UserCookieContext();

        try
        {
            string? Read(string key)
            {
                var value = context.Request.Cookies[key];
                return string.IsNullOrEmpty(value)
                    ? null
                    : _protector.Unprotect(value);
            }

            return new UserCookieContext
            {
                Role = Read("Role"),
                SSA = Read("SSA"),
                Circle = Read("Circle")
            };
        }
        catch
        {
            return new UserCookieContext();
        }
    }
}