using FNS.Repository;
using FNS.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// Register the UserLogin service with scoped lifetime
builder.Services.AddScoped<IUserLogin, UserLogin>();
builder.Services.AddScoped<IImprove, Improve>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddDistributedMemoryCache();

// Register NpgsqlConnection with scoped lifetime (one per request)
builder.Services.AddScoped<NpgsqlConnection>((serviceProvider) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var msg = "Connection string 'DefaultConnection' is not configured. Please set it in appsettings.json or as environment variable 'ConnectionStrings:DefaultConnection'.";
            throw new InvalidOperationException(msg);
        }
        return new NpgsqlConnection(connectionString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
