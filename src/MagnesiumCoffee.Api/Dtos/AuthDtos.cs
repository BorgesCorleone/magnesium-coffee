using System.ComponentModel.DataAnnotations;

namespace MagnesiumCoffee.Api.Dtos;

public record RegistroDto(
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
    string Nome,

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(150)]
    string Email,

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 72 caracteres.")]
    string Senha);

public record LoginDto(
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    string Email,

    [Required(ErrorMessage = "Informe a senha.")]
    string Senha);

public record UsuarioDto(int Id, string Nome, string Email, string Papel);

public record TokenDto(string Token, DateTimeOffset ExpiraEm, UsuarioDto Usuario);
