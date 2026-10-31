namespace MagnesiumCoffee.Api.Dominio;

public class Cafe
{
    public static readonly string[] Torras = ["clara", "media", "escura"];

    public int Id { get; private set; }
    public string Nome { get; private set; } = "";
    public string Produtor { get; private set; } = "";
    public string Municipio { get; private set; } = "";
    public string Variedade { get; private set; } = "";
    public string Processo { get; private set; } = "";
    public string Torra { get; private set; } = "";
    public int AltitudeMetros { get; private set; }
    public decimal PontuacaoSca { get; private set; }
    public int PesoGramas { get; private set; }
    public decimal Preco { get; private set; }
    public int Estoque { get; private set; }
    public DateTime DataTorra { get; private set; }
    public string Descricao { get; private set; } = "";
    public DateTime CriadoEm { get; private set; }

    public List<Pedido> Pedidos { get; private set; } = [];

    private Cafe() { } // EF Core

    public Cafe(string nome, string produtor, string municipio, string variedade, string processo,
        string torra, int altitudeMetros, decimal pontuacaoSca, int pesoGramas, decimal preco,
        int estoque, DateTime dataTorra, string descricao, DateTime criadoEm)
    {
        Atualizar(nome, produtor, municipio, variedade, processo, torra, altitudeMetros,
            pontuacaoSca, pesoGramas, preco, estoque, dataTorra, descricao);
        CriadoEm = Datas.ExigirUtc(criadoEm, nameof(criadoEm));
    }

    public void Atualizar(string nome, string produtor, string municipio, string variedade,
        string processo, string torra, int altitudeMetros, decimal pontuacaoSca, int pesoGramas,
        decimal preco, int estoque, DateTime dataTorra, string descricao)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));
        if (string.IsNullOrWhiteSpace(produtor)) throw new ArgumentException("Produtor é obrigatório.", nameof(produtor));
        if (string.IsNullOrWhiteSpace(municipio)) throw new ArgumentException("Município é obrigatório.", nameof(municipio));
        if (string.IsNullOrWhiteSpace(variedade)) throw new ArgumentException("Variedade é obrigatória.", nameof(variedade));
        if (string.IsNullOrWhiteSpace(processo)) throw new ArgumentException("Processo é obrigatório.", nameof(processo));
        if (!Torras.Contains(torra)) throw new ArgumentException("Torra deve ser clara, media ou escura.", nameof(torra));
        if (altitudeMetros is < 0 or > 3000) throw new ArgumentException("Altitude fora do intervalo.", nameof(altitudeMetros));
        if (pontuacaoSca is < 0 or > 100) throw new ArgumentException("Pontuação SCA deve ficar entre 0 e 100.", nameof(pontuacaoSca));
        if (pesoGramas <= 0) throw new ArgumentException("Peso deve ser positivo.", nameof(pesoGramas));
        if (preco <= 0) throw new ArgumentException("Preço deve ser positivo.", nameof(preco));
        if (estoque < 0) throw new ArgumentException("Estoque não pode ser negativo.", nameof(estoque));

        Nome = nome.Trim();
        Produtor = produtor.Trim();
        Municipio = municipio.Trim();
        Variedade = variedade.Trim();
        Processo = processo.Trim();
        Torra = torra;
        AltitudeMetros = altitudeMetros;
        PontuacaoSca = pontuacaoSca;
        PesoGramas = pesoGramas;
        Preco = preco;
        Estoque = estoque;
        DataTorra = Datas.ExigirUtc(dataTorra, nameof(dataTorra));
        Descricao = descricao?.Trim() ?? "";
    }

    public bool TemEstoque(int quantidade) => quantidade > 0 && quantidade <= Estoque;

    public void BaixarEstoque(int quantidade)
    {
        if (!TemEstoque(quantidade))
            throw new InvalidOperationException("Estoque insuficiente."); // o serviço confere antes (409)
        Estoque -= quantidade;
    }

    public void DevolverEstoque(int quantidade)
    {
        if (quantidade <= 0) throw new ArgumentException("Quantidade deve ser positiva.", nameof(quantidade));
        Estoque += quantidade;
    }
}
