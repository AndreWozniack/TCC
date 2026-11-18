using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoApi.Controllers;

[ApiController]
[Route("api")]
public class InformacoesController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("oi")]
    public IActionResult Ola()
    {
        return Ok("Oi!");
    }

    [Authorize]
    [HttpGet("user")]
    public IActionResult InfoUser()
    {
        return Ok("Informação interna do usuário.");
    }

    [Authorize(Policy = "SomenteAdmin")]
    [HttpGet("admin-status")]
    public IActionResult StatusAdmin()
    {
        return Ok("Status reservado ao administrador.");
    }

    // Falha proposital para a demonstração: falta a política SomenteAdmin.
    [HttpGet("admin-info")]
    public IActionResult InfoAdmin()
    {
        return Ok("Informação importante exclusiva do administrador.");
    }
}
