using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace VitNotaAPI.Controllers;

public record NuevoEstudiante(string Nombres, string Apellidos, DateTime? FechaNacimiento, string? Sexo,
    string? Direccion, string? Telefono, string? CorreoPersonal, int IdGrado, int NumeroSeccion);

public record NuevoDocente(string Nombres, string Apellidos, string? Especialidad,
    string? Telefono, string? CorreoPersonal);

[ApiController]
[Route("api")]
public class PersonasController : ControllerBase
{
    private readonly SqlConnection _db;
    public PersonasController(SqlConnection db) => _db = db;

    // Convierte filas de Dapper en diccionarios que se pueden enviar como JSON
    static IEnumerable<Dictionary<string, object>> Filas(IEnumerable<dynamic> filas) =>
        filas.Select(f => new Dictionary<string, object>((IDictionary<string, object>)f));

    [HttpGet("grados")]
    public async Task<IActionResult> Grados() =>
        Ok(Filas(await _db.QueryAsync(
            "SELECT id_grado, nombre_grado FROM dbo.Grado WHERE estado = 1 ORDER BY id_grado")));

    [HttpGet("estudiantes")]
    public async Task<IActionResult> Estudiantes() =>
        Ok(Filas(await _db.QueryAsync(@"
            SELECT e.id_estudiante, e.codigo, e.nombres, e.apellidos, u.correo,
                   g.nombre_grado, s.nombre_seccion
            FROM dbo.Estudiante e
            JOIN dbo.Usuario u ON u.id_usuario = e.id_usuario
            LEFT JOIN dbo.Matricula m ON m.id_estudiante = e.id_estudiante
                 AND m.id_anio = (SELECT id_anio FROM dbo.AnioLectivo WHERE activo = 1)
            LEFT JOIN dbo.Seccion s ON s.id_seccion = m.id_seccion
            LEFT JOIN dbo.Grado g ON g.id_grado = s.id_grado
            ORDER BY e.id_estudiante DESC")));

    [HttpPost("estudiantes")]
    public async Task<IActionResult> CrearEstudiante([FromBody] NuevoEstudiante d)
    {
        try
        {
            var r = await _db.QueryFirstAsync("dbo.agregar_estudiante", new
            {
                nombres = d.Nombres,
                apellidos = d.Apellidos,
                fecha_nacimiento = d.FechaNacimiento,
                sexo = d.Sexo,
                direccion = d.Direccion,
                telefono = d.Telefono,
                correo_personal = d.CorreoPersonal,
                id_grado = d.IdGrado,
                numero_seccion = d.NumeroSeccion
            }, commandType: CommandType.StoredProcedure);

            return Ok(new Dictionary<string, object>((IDictionary<string, object>)r));
        }
        catch (SqlException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpGet("docentes")]
    public async Task<IActionResult> Docentes() =>
        Ok(Filas(await _db.QueryAsync(@"
            SELECT d.id_docente, d.codigo, d.nombres, d.apellidos, d.especialidad, u.correo
            FROM dbo.Docente d
            JOIN dbo.Usuario u ON u.id_usuario = d.id_usuario
            ORDER BY d.id_docente DESC")));

    [HttpPost("docentes")]
    public async Task<IActionResult> CrearDocente([FromBody] NuevoDocente d)
    {
        try
        {
            var r = await _db.QueryFirstAsync("dbo.agregar_docente", new
            {
                nombres = d.Nombres,
                apellidos = d.Apellidos,
                especialidad = d.Especialidad,
                telefono = d.Telefono,
                correo_personal = d.CorreoPersonal
            }, commandType: CommandType.StoredProcedure);

            return Ok(new Dictionary<string, object>((IDictionary<string, object>)r));
        }
        catch (SqlException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}