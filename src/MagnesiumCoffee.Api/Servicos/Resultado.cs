namespace MagnesiumCoffee.Api.Servicos;

public enum TipoErro
{
    Validacao,      // 400
    NaoAutenticado, // 401 (login inválido)
    Proibido,       // 403
    NaoEncontrado,  // 404
    Conflito,       // 409
}

// Erro de negócio não usa exceção: o serviço devolve Resultado e o controller traduz para HTTP.
public sealed class Resultado<T>
{
    public bool Sucesso { get; }
    public T? Valor { get; }
    public TipoErro? Erro { get; }
    public string? Mensagem { get; }

    private Resultado(bool sucesso, T? valor, TipoErro? erro, string? mensagem)
    {
        Sucesso = sucesso;
        Valor = valor;
        Erro = erro;
        Mensagem = mensagem;
    }

    public static Resultado<T> Ok(T valor) => new(true, valor, null, null);

    public static Resultado<T> Falha(TipoErro erro, string mensagem) => new(false, default, erro, mensagem);
}
