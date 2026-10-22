// =====================================================================
//  Give-AID NGO Website - application entry point
//  Configures services (database, authentication, MVC) and the
//  HTTP request pipeline, then creates/seeds the database on start-up.
// =====================================================================
using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Services;
using System.IO.Compression;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- MVC (controllers + Razor views) ----------
builder.Services.AddControllersWithViews();

// ---------- Smaller downloads: every page, CSS and JS file is sent compressed ----------
// Brotli for modern browsers, Gzip for older ones (usually 70-80% smaller text files).
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "image/svg+xml", "text/javascript" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);

// ---------- Database: SQL Server through Entity Framework Core ----------
builder.Services.AddDbContext<GiveAidContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GiveAidDB")));

// ---------- Authentication ----------
// Two separate login cookies are used so that the ADMIN module and the
// USER (member) module are completely independent of each other.
builder.Services.AddAuthentication(AuthSchemes.Member)
    .AddCookie(AuthSchemes.Member, options =>
    {
        options.Cookie.Name = "GiveAid.Member";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    })
    .AddCookie(AuthSchemes.Admin, options =>
    {
        options.Cookie.Name = "GiveAid.Admin";
        options.LoginPath = "/Admin/Auth/Login";
        options.AccessDeniedPath = "/Admin/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// ---------- Application services ----------
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<FileStorage>();

// ---------- AI Programme Planner (admin) ----------
// Settings: "Planner" section of appsettings.json. Default "Demo" works offline without an API key.
builder.Services.Configure<GiveAid.Services.Planner.PlannerOptions>(builder.Configuration.GetSection("Planner"));
builder.Services.AddHttpClient("openmeteo", c =>
{
    c.Timeout = TimeSpan.FromSeconds(10);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("GiveAID-Planner/1.0");
});
builder.Services.AddHttpClient("llm");
builder.Services.AddScoped<GiveAid.Services.Planner.PlannerToolbox>();
builder.Services.AddScoped<GiveAid.Services.Planner.PlannerService>();

var app = builder.Build();

// ---------- Create database (first run) and insert sample data ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GiveAidContext>();
    db.Database.EnsureCreated();   // creates GiveAidDB + all tables if they do not exist
    SchemaUpgrader.Upgrade(db);    // adds tables/columns that newer versions need to an older GiveAidDB
    DbSeeder.Seed(db);             // inserts default admin, causes, pages, etc. (only when empty)
}

// ---------- HTTP pipeline ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage");

app.UseHttpsRedirection();
app.UseResponseCompression();
// Static files are cached by the browser for 30 days. Files linked with asp-append-version="true"
// get a new ?v= value whenever they change, so visitors still get updates straight away.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public,max-age=2592000";
    }
});
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Admin area route:  /Admin/{controller}/{action}/{id?}
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// About Us sub pages:  /About/our-mission
app.MapControllerRoute(
    name: "about",
    pattern: "About/{slug?}",
    defaults: new { controller = "About", action = "Index" });

// Default user route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
