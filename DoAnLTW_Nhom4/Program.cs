using DoAnLTW_Nhom4.Repositories.EFRepositories;
using DoAnLTW_Nhom4.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DoAnLTW_Nhom4.Data;
using DoAnLTW_Nhom4.Models;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddTransient<IEmailSender, EmailSender>();


builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => { options.SignIn.RequireConfirmedAccount = false; })
    .AddDefaultTokenProviders()
    .AddDefaultUI()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();

// Dăng kí reposotory
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();
builder.Services.AddScoped<IBrandRepository, EFBrandRepository>();
builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();
builder.Services.AddScoped<IProductSpecificationRepository, EFProductSpecificationReposotory>();
builder.Services.AddScoped<RoleManager<IdentityRole>>();
builder.Services.AddScoped<IReviewRepository, EFReviewRepository>();


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});
// 🔹 Cấu hình CookiePolicy cho OAuth callback
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // Cho phép cookie gửi kèm trong các yêu cầu cross-site (như OAuth callback)
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
});

// 🔹 Tinh chỉnh cookie (SameSite=None, HTTPS)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
// Đăng nhập bằng fb và GG
builder.Services.AddAuthentication()
    .AddGoogle(options => {
        options.ClientId = "382000743151-saq98iqdj2t5jb0e4q4r3hos340jpf88.apps.googleusercontent.com";
        options.ClientSecret = "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";
    });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map Razor Pages first (for Identity)
app.MapRazorPages();

// Then map controller routes
app.MapControllerRoute(
    name: "Employee",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await DataSeeder.SeedRolesAsync(roleManager);
    await DataSeeder.SeedAdminAsync(userManager);
}
app.Run();
