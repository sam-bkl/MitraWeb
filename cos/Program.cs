using Microsoft.AspNetCore.Authentication.Cookies;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using YourProject.Repositories.Interfaces;
using cos.Repositories;
using CosApp.Infra;  //*****************//
using cos.Services;
using cos.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddConfiguredForwardedHeaders();  //*****************//
builder.Services.AddMemoryCache();
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAntiforgery(o => o.HeaderName = "XSRF-TOKEN");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => {
    options.LoginPath = "/Home/login";
    options.AccessDeniedPath = "/Home/AccessDenied";

});

builder.Services.AddHttpContextAccessor();
//inject user role,ba,ssa into  UserCookieContext
builder.Services.AddScoped<IUserCookieContextAccessor,UserCookieContextAccessor>();

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.Secure = CookieSecurePolicy.None;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    //options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
    options.CheckConsentNeeded = context => false;
    options.OnAppendCookie = cookieContext =>
    {
        cookieContext.CookieOptions.Secure = true;  // Secure attribute for sensitive cookies
        cookieContext.CookieOptions.HttpOnly = true;
    };

});

builder.Services.AddScoped<IOracleRepository, OracleRepository>();
builder.Services.AddScoped<SummaryRepository, SummaryRepository>();
builder.Services.AddScoped<PosRepository, PosRepository>();

// Register HttpClient for FileStoreService
builder.Services.AddHttpClient();

// Register FileStoreService
builder.Services.AddScoped<cos.Services.FileStoreService>();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/home/bsnlcos/keys"))
    .SetApplicationName("bcos-app");

IWebHostEnvironment _env = builder.Environment;
builder.Services.AddDNTCaptcha(options =>
{
    
    // options.UseSessionStorageProvider() // -> It doesn't rely on the server or client's times. Also it's the safest one.
    // options.UseMemoryCacheStorageProvider() // -> It relies on the server's times. It's safer than the CookieStorageProvider.
    options.UseCookieStorageProvider(SameSiteMode.Strict /* If you are using CORS, set it to `None` */) // -> It relies on the server and client's times. It's ideal for scalability, because it doesn't save anything in the server's memory.
                                                                                                        // .UseDistributedCacheStorageProvider() // --> It's ideal for scalability using `services.AddStackExchangeRedisCache()` for instance.
                                                                                                        // .UseDistributedSerializationProvider()

    // Don't set this line (remove it) to use the installed system's fonts (FontName = "Tahoma").
    // Or if you want to use a custom font, make sure that font is present in the wwwroot/fonts folder and also use a good and complete font!
    .UseCustomFont(Path.Combine(_env.WebRootPath, "fonts", "Crashnumberingserif-KVjW.ttf"))
    .AbsoluteExpiration(minutes: 7)
    .ShowThousandsSeparators(false)
    .WithNoise(pixelsDensity: 25, linesCount: 3)
    .WithEncryptionKey("This is my secure key!")
    .InputNames(// This is optional. Change it if you don't like the default names.
        new DNTCaptchaComponent
        {
            CaptchaHiddenInputName = "DNT_CaptchaText",
            CaptchaHiddenTokenName = "DNT_CaptchaToken",
            CaptchaInputName = "DNT_CaptchaInputText"
        })
    .Identifier("dnt_Captcha")// This is optional. Change it if you don't like its default name.
    ;
});

var app = builder.Build();
app.UseForwardedHeaders();  //*****************//
//// Apply /portal ONLY in Production
//if (app.Environment.IsProduction())
//{
//    app.UsePathBase("/portal");
//}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // even in development, if you want custom error page:
    app.UseExceptionHandler("/Home/Error");
}

//var forwardedHeadersOptions = new ForwardedHeadersOptions
//{
//    ForwardedHeaders = ForwardedHeaders.All
//};
//forwardedHeadersOptions.KnownNetworks.Clear();
//forwardedHeadersOptions.KnownProxies.Clear();

//app.UseForwardedHeaders(forwardedHeadersOptions);


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();   
app.UseAuthorization();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
