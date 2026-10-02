using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace VitNotaAPI.Controllers;

public record LoginSolicitud(string Correo, string Contrasena);
public record RecuperacionSolicitud(string Correo);
public record RestablecerSolicitud(string Correo, string Codigo, string NuevaContrasena);
public record CambiarSolicitud(int IdUsuario, string Actual, string Nueva);

public class LoginRespuesta
{
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string Correo { get; set; } = "";
    public string NombreRol { get; set; } = "";
    public bool DebeCambiarContrasena { get; set; }
}

public class CodigoRecuperacion
{
    public string EnviarA { get; set; } = "";
    public string Codigo { get; set; } = "";
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SqlConnection _db;
    private readonly ILogger<AuthController> _log;
    private readonly CorreoServicio _correo;

    public AuthController(SqlConnection db, ILogger<AuthController> log, CorreoServicio correo)
    {
        _db = db;
        _log = log;
        _correo = correo;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginSolicitud d)
    {
        if (string.IsNullOrWhiteSpace(d.Correo) || string.IsNullOrWhiteSpace(d.Contrasena))
            return BadRequest(new { mensaje = "Escribe tu correo y contraseña." });

        var u = await _db.QueryFirstOrDefaultAsync<LoginRespuesta>(
            "dbo.login_vitnota",
            new { correo = d.Correo.Trim().ToLower(), contrasena = d.Contrasena },
            commandType: CommandType.StoredProcedure);

        return u is null
            ? Unauthorized(new { mensaje = "Correo o contraseña incorrectos." })
            : Ok(u);
    }

    // Paso 1: pide el código. Responde igual exista o no el correo.
    [HttpPost("solicitar-recuperacion")]
    public async Task<IActionResult> Solicitar([FromBody] RecuperacionSolicitud d)
    {
        var r = await _db.QueryFirstOrDefaultAsync<CodigoRecuperacion>(
            "dbo.solicitar_recuperacion",
            new { correo = d.Correo.Trim().ToLower() },
            commandType: CommandType.StoredProcedure);

        if (r is not null)
        {
            var html = $@"<div style='font-family:Segoe UI,Arial,sans-serif;max-width:420px;margin:auto;padding:28px;border:1px solid #e4e0d6;border-radius:14px'>
                <h2 style='margin:0 0 6px;color:#14181f'>Vit<span style='color:#e0593c'>Nota</span></h2>
                <p style='color:#555'>Recibimos una solicitud para cambiar tu contraseña. Tu código es:</p>
                <p style='font-size:34px;letter-spacing:8px;font-weight:700;color:#0e7c66;margin:18px 0'>{r.Codigo}</p>
                <p style='color:#777;font-size:13px'>Vence en 15 minutos. Si no lo pediste, ignora este mensaje.</p></div>";

            var enviado = await _correo.EnviarAsync(r.EnviarA, "Tu código de recuperación - VitNota", html);

            // Respaldo: si el correo no salió, el código queda en la terminal.
            if (!enviado)
                _log.LogWarning("CÓDIGO DE RECUPERACIÓN para {Destino}: {Codigo}", r.EnviarA, r.Codigo);
        }
        return Ok(new { mensaje = "Si el correo existe, se envió un código." });
    }

    // Paso 2: usa el código y cambia la contraseña.
    [HttpPost("restablecer")]
    public async Task<IActionResult> Restablecer([FromBody] RestablecerSolicitud d)
    {
        try
        {
            await _db.ExecuteAsync(
                "dbo.restablecer_contrasena",
                new { correo = d.Correo.Trim().ToLower(), codigo = d.Codigo, nueva_contrasena = d.NuevaContrasena },
                commandType: CommandType.StoredProcedure);
            return Ok(new { mensaje = "Contraseña cambiada." });
        }
        catch (SqlException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    // Cambio obligatorio del primer acceso.
    [HttpPost("cambiar")]
    public async Task<IActionResult> Cambiar([FromBody] CambiarSolicitud d)
    {
        try
        {
            await _db.ExecuteAsync(
                "dbo.cambiar_contrasena",
                new { id_usuario = d.IdUsuario, actual = d.Actual, nueva = d.Nueva },
                commandType: CommandType.StoredProcedure);
            return Ok(new { mensaje = "Contraseña actualizada." });
        }
        catch (SqlException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}
