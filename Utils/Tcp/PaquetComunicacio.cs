using Utils.Cripto.Simetric;

namespace Utils.Tcp
{
    public record PaquetComunicacio
    {
        public string Tipus { get; init; } = string.Empty;
        public string Nickname { get; init; } = string.Empty;
        public string Contingut { get; init; } = string.Empty;
        public AlgorismeSimetric? XifratTriat { get; init; }
        public string? DadesClauCriptografica { get; init; }
    }
}
