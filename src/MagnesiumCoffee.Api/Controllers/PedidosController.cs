using MagnesiumCoffee.Api.Dtos;
using MagnesiumCoffee.Api.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MagnesiumCoffee.Api.Controllers;

[Authorize]
[Route("api/pedidos")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class PedidosController(PedidoServico servico) : ApiBaseController
{
    /// <summary>Faz um pedido. Quantidade maior que o estoque devolve 409.</summary>
    [HttpPost]
    [ProducesResponseType<PedidoSaidaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PedidoSaidaDto>> Criar(PedidoEntradaDto dto)
    {
        var resultado = await servico.CriarAsync(dto, UsuarioId);
        if (!resultado.Sucesso) return Erro(resultado);
        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    /// <summary>Lista pedidos: o cliente vê só os seus; o admin vê todos.</summary>
    /// <param name="status">Filtro opcional: pendente ou cancelado.</param>
    /// <param name="pagina">Página, a partir de 1.</param>
    /// <param name="tamanho">Itens por página (1 a 50).</param>
    [HttpGet]
    [ProducesResponseType<PaginaDto<PedidoSaidaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaDto<PedidoSaidaDto>>> Listar(
        [FromQuery] string? status, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 10) =>
        Ok(await servico.ListarAsync(UsuarioId, EhAdmin, status, pagina, tamanho));

    /// <summary>Detalhe de um pedido (dono ou admin).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PedidoSaidaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoSaidaDto>> ObterPorId(int id)
    {
        var resultado = await servico.ObterAsync(id, UsuarioId, EhAdmin);
        return resultado.Sucesso ? Ok(resultado.Valor) : Erro(resultado);
    }

    /// <summary>Cancela um pedido e devolve os pacotes ao estoque (dono ou admin).</summary>
    [HttpPost("{id:int}/cancelar")]
    [ProducesResponseType<PedidoSaidaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PedidoSaidaDto>> Cancelar(int id)
    {
        var resultado = await servico.CancelarAsync(id, UsuarioId, EhAdmin);
        return resultado.Sucesso ? Ok(resultado.Valor) : Erro(resultado);
    }
}
