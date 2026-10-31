using MagnesiumCoffee.Api.Dados;
using MagnesiumCoffee.Api.Dominio;
using MagnesiumCoffee.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MagnesiumCoffee.Api.Servicos;

public class AuthServico(AppDbContext db, TokenServico tokens, ILogger<AuthServico> logger)
{
    public async Task<Resultado<TokenDto>> RegistrarAsync(RegistroDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.Email == email))
        {
            logger.LogWarning("Registro recusado: e-mail {Email} já cadastrado", email);
            return Resultado<TokenDto>.Falha(TipoErro.Conflito, $"Já existe uma conta com o e-mail {email}.");
        }

        // O registro público sempre cria "cliente"; admin só nasce no seed.
        var usuario = new Usuario(dto.Nome, email, BCrypt.Net.BCrypt.HashPassword(dto.Senha),
            Papeis.Cliente, DateTime.UtcNow);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        logger.LogInformation("Usuário {UsuarioId} registrado como {Papel}", usuario.Id, usuario.Papel);
        return Resultado<TokenDto>.Ok(CriarToken(usuario));
    }

    public async Task<Resultado<TokenDto>> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Email == email);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash))
        {
            logger.LogWarning("Falha de login para {Email}", email);
            return Resultado<TokenDto>.Falha(TipoErro.NaoAutenticado, "E-mail ou senha inválidos.");
        }

        logger.LogInformation("Usuário {UsuarioId} fez login", usuario.Id);
        return Resultado<TokenDto>.Ok(CriarToken(usuario));
    }

    public async Task<Resultado<UsuarioDto>> ObterAsync(int usuarioId)
    {
        var usuario = await db.Usuarios.FindAsync(usuarioId);
        return usuario is null
            ? Resultado<UsuarioDto>.Falha(TipoErro.NaoEncontrado, $"Usuário {usuarioId} não encontrado.")
            : Resultado<UsuarioDto>.Ok(ParaDto(usuario));
    }

    private TokenDto CriarToken(Usuario usuario)
    {
        var (token, expiraEm) = tokens.Gerar(usuario);
        return new TokenDto(token, expiraEm, ParaDto(usuario));
    }

    private static UsuarioDto ParaDto(Usuario u) => new(u.Id, u.Nome, u.Email, u.Papel);
}
