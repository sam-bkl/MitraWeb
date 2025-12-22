using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using YourProject.Repositories.Interfaces;
using cos.Repositories;
using CosApp.Infra;  //*****************//

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

// Register HttpContextAccessor for views
builder.Services.AddHttpContextAccessor();

// Register HttpClient for FileStoreService
builder.Services.AddHttpClient();

// Register FileStoreService
builder.Services.AddScoped<cos.Services.FileStoreService>();

// Register CaptchaService
builder.Services.AddSingleton<cos.Services.CaptchaService>();

// Register SMS API Caller
builder.Services.AddScoped<cos.ApiCallers.SmsApiCaller>();

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(6); // Slightly more than OTP expiry
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/home/bsnlcos/keys"))
    .SetApplicationName("bcos-app");

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
app.UseCookiePolicy();
app.UseAuthentication();   
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
