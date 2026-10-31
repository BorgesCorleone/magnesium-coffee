using System.ComponentModel.DataAnnotations;

namespace MagnesiumCoffee.Api.Dtos;

/// <summary>Dados para fazer um pedido. O dono é sempre o usuário do token.</summary>
public record PedidoEntradaDto(
    [Range(1, int.MaxValue, ErrorMessage = "Informe um café válido.")]
    int CafeId,

    [Range(1, 50, ErrorMessage = "A quantidade deve ficar entre 1 e 50 pacotes.")]
    int Quantidade,

    [Required(ErrorMessage = "Informe o endereço de entrega.")]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "O endereço deve ter entre 10 e 200 caracteres.")]
    string EnderecoEntrega);

/// <summary>Pedido como é devolvido pela API.</summary>
public record PedidoSaidaDto(
    int Id,
    int CafeId,
    string CafeNome,
    int UsuarioId,
    string ClienteNome,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Total,
    string EnderecoEntrega,
    string Status,
    DateTimeOffset CriadoEm);
