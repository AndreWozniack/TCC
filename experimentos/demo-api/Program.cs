using DemoApi.Seguranca;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication("Demo")
    .AddScheme<AuthenticationSchemeOptions, AutenticacaoDemo>("Demo", _ => { });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("SomenteAdmin", policy => policy.RequireRole("Admin")));
builder.Services.AddControllers();
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
