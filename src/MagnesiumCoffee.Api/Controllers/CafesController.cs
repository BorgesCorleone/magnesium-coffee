using MagnesiumCoffee.Api.Dominio;
using MagnesiumCoffee.Api.Dtos;
using MagnesiumCoffee.Api.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MagnesiumCoffee.Api.Controllers;

[Route("api/cafes")]
public class CafesController(CafeServico servico) : ApiBaseController
{
    /// <summary>Catálogo público, paginado, com busca por nome, produtor, município, variedade ou processo.</summary>
    /// <param name="busca">Termo de busca (ex.: bourbon, iúna, fermentado).</param>
    /// <param name="pagina">Página, a partir de 1.</param>
    /// <param name="tamanho">Itens por página (1 a 50).</param>
    [HttpGet]
    [ProducesResponseType<PaginaDto<CafeSaidaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaDto<CafeSaidaDto>>> Listar(
        [FromQuery] string? busca, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 10) =>
        Ok(await servico.ListarAsync(busca, pagina, tamanho));

    /// <summary>Detalhe de um café.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<CafeSaidaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CafeSaidaDto>> ObterPorId(int id)
    {
        var resultado = await servico.ObterAsync(id);
        return resultado.Sucesso ? Ok(resultado.Valor) : Erro(resultado);
    }

    /// <summary>Cadastra um café (somente admin).</summary>
    [Authorize(Roles = Papeis.Admin)]
    [HttpPost]
    [ProducesResponseType<CafeSaidaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CafeSaidaDto>> Criar(CafeEntradaDto dto)
    {
        var resultado = await servico.CriarAsync(dto);
        if (!resultado.Sucesso) return Erro(resultado);
        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    /// <summary>Atualiza um café (somente admin).</summary>
    [Authorize(Roles = Papeis.Admin)]
    [HttpPut("{id:int}")]
    [ProducesResponseType<CafeSaidaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CafeSaidaDto>> Atualizar(int id, CafeEntradaDto dto)
    {
        var resultado = await servico.AtualizarAsync(id, dto);
        return resultado.Sucesso ? Ok(resultado.Valor) : Erro(resultado);
    }

    /// <summary>Exclui um café sem pedidos (somente admin). Com pedidos, devolve 409.</summary>
    [Authorize(Roles = Papeis.Admin)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Excluir(int id)
    {
        var resultado = await servico.ExcluirAsync(id);
        return resultado.Sucesso ? NoContent() : Erro(resultado);
    }
}
