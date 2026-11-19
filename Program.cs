using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using PowerGym.Data;
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<PowerGym.Data.VentaDAO>();
builder.Services.AddScoped<PowerGym.Data.VendedorDAO>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Acceso/Entrar";
        options.AccessDeniedPath = "/Acceso/Denegado";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Localhost HTTP para el proyecto. En producción usar Always y HTTPS.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<SocioDAO>();
builder.Services.AddScoped<MembresiaDAO>();
var app=builder.Build();

// Alta local por terminal: nunca expone una pantalla pública de registro.
if (args.Contains("--crear-usuario"))
{
    using var scope = app.Services.CreateScope();
    var dao = scope.ServiceProvider.GetRequiredService<VendedorDAO>();
    Console.Write("Usuario (letras/numeros, minimo 3): ");
    var usuario = Console.ReadLine() ?? "";
    Console.Write("Nombre del empleado: ");
    var nombre = Console.ReadLine() ?? "";
    Console.Write("Rol (VENDEDOR o AUDITOR): ");
    var rol = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
    string clave;
    while (true)
    {
        Console.Write("Contrasena (minimo 12 caracteres): ");
        var caracteres = new System.Text.StringBuilder();

        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);

            if (tecla.Key == ConsoleKey.Enter)
                break;

            if (tecla.Key == ConsoleKey.Backspace)
            {
                if (caracteres.Length > 0)
                {
                    caracteres.Length--;
                    Console.Write("\b \b");
                }
            }
            else if (!char.IsControl(tecla.KeyChar) && caracteres.Length < 128)
            {
                caracteres.Append(tecla.KeyChar);
                Console.Write("*");
            }
        }

        Console.WriteLine();

        if (caracteres.Length >= 12)
        {
            clave = caracteres.ToString();
            break;
        }

        Console.WriteLine("Contrasena demasiado corta. Intenta nuevamente.");
    }

    await dao.CrearUsuarioLocalAsync(usuario, nombre, rol, clave);
    Console.WriteLine("Usuario creado correctamente. Ya puede iniciar sesion.");
    return;
}
if(!app.Environment.IsDevelopment()){
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name:"default",pattern:"{controller=Home}/{action=Index}/{id?}");
app.Run();
