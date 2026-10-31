using MagnesiumCoffee.Api.Dtos;
using MagnesiumCoffee.Api.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MagnesiumCoffee.Api.Controllers;

[Route("api/auth")]
public class AuthController(AuthServico servico) : ApiBaseController
{
    /// <summary>Cria uma conta de cliente e já devolve o token.</summary>
    [HttpPost("registro")]
    [ProducesResponseType<TokenDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TokenDto>> Registrar(RegistroDto dto)
    {
        var resultado = await servico.RegistrarAsync(dto);
        if (!resultado.Sucesso) return Erro(resultado);
        return CreatedAtAction(nameof(Eu), null, resultado.Valor);
    }

    /// <summary>Faz login e devolve o JWT (cole só o token no botão Authorize).</summary>
    [HttpPost("login")]
    [ProducesResponseType<TokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenDto>> Login(LoginDto dto)
    {
        var resultado = await servico.LoginAsync(dto);
        return resultado.Sucesso ? Ok(resultado.Valor) : Erro(resultado);
    }

    /// <summary>Dados do usuário dono do token.</summary>
    [Authorize]
    [HttpGet("eu")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UsuarioDto>> Eu()
    {
        var resultado = await servico.ObterAsync(UsuarioId);
        return resultado.Sucesso ? Ok(resultado.Valor) : Erro(resultado);
    }
}
