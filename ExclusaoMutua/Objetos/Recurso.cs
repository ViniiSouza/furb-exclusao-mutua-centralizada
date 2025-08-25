namespace ExclusaoMutua.Objetos;

public class Recurso
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool EmUso { get; set; }
    // qualquer outro atributo que seja necessário
}