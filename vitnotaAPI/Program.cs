using Dapper;
using Microsoft.Data.SqlClient;

// Permite que columnas como id_usuario se conviertan en IdUsuario
DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Conexión a SQL Server (sale de user-secrets, no de GitHub)
var cadena = builder.Configuration.GetConnectionString("VitNota")
    ?? throw new InvalidOperationException("Falta la conexión 'VitNota' en user-secrets.");
builder.Services.AddScoped(_ => new SqlConnection(cadena));

// Envío de correos (Gmail)
builder.Services.AddScoped<VitNotaAPI.CorreoServicio>();

var app = builder.Build();

app.UseHttpsRedirection();

// Sirve index.html desde la carpeta wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();