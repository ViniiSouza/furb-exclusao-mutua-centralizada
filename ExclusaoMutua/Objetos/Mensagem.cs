namespace ExclusaoMutua.Objetos;

public record Mensagem(ETipoMensagem Tipo, long? RecursoId, int ProcessoId);