using System.Security.Claims;
using MagnesiumCoffee.Api.Dominio;
using MagnesiumCoffee.Api.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace MagnesiumCoffee.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiBaseController : ControllerBase
{
    // Identidade sempre vem dos claims do token, nunca do corpo da requisição.
    protected int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected bool EhAdmin => User.IsInRole(Papeis.Admin);

    protected ObjectResult Erro<T>(Resultado<T> resultado) => resultado.Erro switch
    {
        TipoErro.Validacao => Problem(statusCode: 400, title: "Dados inválidos", detail: resultado.Mensagem),
        TipoErro.NaoAutenticado => Problem(statusCode: 401, title: "Não autenticado", detail: resultado.Mensagem),
        TipoErro.Proibido => Problem(statusCode: 403, title: "Acesso negado", detail: resultado.Mensagem),
        TipoErro.NaoEncontrado => Problem(statusCode: 404, title: "Não encontrado", detail: resultado.Mensagem),
        TipoErro.Conflito => Problem(statusCode: 409, title: "Conflito", detail: resultado.Mensagem),
        _ => Problem(statusCode: 500, title: "Erro inesperado", detail: resultado.Mensagem),
    };
}
