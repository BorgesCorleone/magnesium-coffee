using System.ComponentModel.DataAnnotations;

namespace MagnesiumCoffee.Api.Dtos;

/// <summary>Dados para cadastrar ou atualizar um café.</summary>
public record CafeEntradaDto(
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
    string Nome,

    [Required(ErrorMessage = "Informe o produtor.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O produtor deve ter entre 3 e 100 caracteres.")]
    string Produtor,

    [Required(ErrorMessage = "Informe o município.")]
    [StringLength(60, MinimumLength = 2)]
    string Municipio,

    [Required(ErrorMessage = "Informe a variedade.")]
    [StringLength(60, MinimumLength = 2)]
    string Variedade,

    [Required(ErrorMessage = "Informe o processo.")]
    [StringLength(60, MinimumLength = 2)]
    string Processo,

    [Required(ErrorMessage = "Informe a torra.")]
    [AllowedValues("clara", "media", "escura", ErrorMessage = "A torra deve ser clara, media ou escura.")]
    string Torra,

    [Range(0, 3000, ErrorMessage = "A altitude deve ficar entre 0 e 3000 m.")]
    int AltitudeMetros,

    [Range(typeof(decimal), "0", "100", ErrorMessage = "A pontuação SCA deve ficar entre 0 e 100.")]
    decimal PontuacaoSca,

    [Range(50, 5000, ErrorMessage = "O peso deve ficar entre 50 g e 5000 g.")]
    int PesoGramas,

    [Range(typeof(decimal), "0.01", "10000", ErrorMessage = "O preço deve ficar entre 0,01 e 10.000.")]
    decimal Preco,

    [Range(0, 100000, ErrorMessage = "O estoque deve ficar entre 0 e 100.000.")]
    int Estoque,

    [Required(ErrorMessage = "Informe a data da torra.")]
    DateTimeOffset? DataTorra,

    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    string? Descricao);

/// <summary>Café como é devolvido pela API.</summary>
public record CafeSaidaDto(
    int Id,
    string Nome,
    string Produtor,
    string Municipio,
    string Variedade,
    string Processo,
    string Torra,
    int AltitudeMetros,
    decimal PontuacaoSca,
    int PesoGramas,
    decimal Preco,
    int Estoque,
    bool Disponivel,
    DateTimeOffset DataTorra,
    string Descricao);
