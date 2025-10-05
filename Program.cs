using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HortalisCSharp.Data;
using HortalisCSharp.Models;

namespace HortalisCSharp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews(); // MVC (se usar Razor Pages, também adicione: AddRazorPages)
            builder.Services.AddDbContext<AppDbContext>(opt =>
                opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o =>
                {
                    o.LoginPath = "/Login/Index";
                    o.AccessDeniedPath = "/Login/Index";
                    o.ExpireTimeSpan = TimeSpan.FromHours(2);
                    o.SlidingExpiration = true;
                });

            var app = builder.Build();

            // Aplica migrations automaticamente em dev/execução
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapDefaultControllerRoute();
            // app.MapRazorPages(); // caso use Razor Pages também

            app.Run();
        }
    }
}
