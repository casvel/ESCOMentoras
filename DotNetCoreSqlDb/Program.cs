using Microsoft.EntityFrameworkCore;
using DotNetCoreSqlDb.Data;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web.UI;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using DotNetCoreSqlDb.Hubs;
using Microsoft.Extensions.Primitives;

var builder = WebApplication.CreateBuilder(args);

// Add database context and cache
builder.Services.AddDbContext<MyDatabaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING")));
//builder.Services.AddDbContext<MyDatabaseContext>(options => 
  //  options.UseSqlServer(builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING_LOCAL")));

if (!builder.Environment.IsDevelopment())
{
    // Add Redis cache only for non-development
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration["AZURE_REDIS_CONNECTIONSTRING"];
        options.InstanceName = "ESCOMentoras";
    });
}

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
  .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdToken"));
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var existingOnMessageReceivedHandler = options.Events.OnMessageReceived;
    options.Events.OnMessageReceived = async context =>
    {
        await existingOnMessageReceivedHandler(context);

        StringValues accessToken = context.Request.Headers.Authorization;
        PathString path = context.HttpContext.Request.Path;

        // If the request is for our hub...
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chat"))
        {
            // Read the token out of the Authorization header
            context.Token = accessToken;
        }
    };
});

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
              .RequireAuthenticatedUser()
              .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme)
              .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});
builder.Services.AddRazorPages();
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.Secure = CookieSecurePolicy.Always;
});

// Add App Service logging
builder.Logging.AddAzureWebAppDiagnostics();

builder.Services.AddSignalR();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();


// Customize the authentication challenge behavior to return a 401 status code.
app.Use(async (context, next) =>
{
    await next();
 
    app.Logger.LogInformation(String.Format("Header contains application/json: {0}. Value: {1}", context.Request.Headers.Accept.Contains("application/json"), context.Request.Headers.Accept));
    if (!context.User.Identity.IsAuthenticated && context.Request.Headers.Accept.Contains("application/json"))
    {
        // Return a 401 status code instead of redirecting to the login page.
        context.Response.StatusCode = 401; // Unauthorized
        return;
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Profiles}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHub<ChatHub>("/chat");
app.MapHub<NotificationsHub>("/notifications");

app.Run();
