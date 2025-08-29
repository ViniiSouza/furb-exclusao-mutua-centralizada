namespace ExclusaoMutua.Objetos;

public class Recurso
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool EmUso { get; set; }
    public int IdProcessoUtilizando { get; set; }
    public DateTime HoraInicioUso { get; set; }
}