// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Sockets;

Console.WriteLine("Hello, World!");


// processo

// recurso

// coordenador

// requisicao
// contem processo e recurso

public class Processo
{
    public int Id { get; set; }
    public bool SouCoordenador { get; set; }
    public int Coordenador { get; set; }
    public bool AguardandoRecurso { get; set; }
    public Queue<Mensagem> FilaRequisicoes { get; set; } = new();
    // seria uma lista de recursos, mas para simplificar
    public Recurso Recurso { get; set; }
    public Thread ThreadRecurso { get; set; }

    public Processo(int id, int coordenadorId, Recurso recurso)
    {
        Id = id;
        Coordenador = coordenadorId;
        Recurso = recurso;
    }

    public void IniciaServidor(IPAddress ipAddress)
    {
        TcpListener listener = new TcpListener(ipAddress, 5000 + Id);
        listener.Start();
        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
        }
    }

    public void RecebeCoordenador(int coordenadorId)
    {
        Coordenador = coordenadorId;
        SouCoordenador = Id == coordenadorId;
    }

    public void IniciaRequisitador()
    {
        var random = new Random();
        while (true)
        {
            if (!AguardandoRecurso)
            {
                SolicitaRecurso(Recurso);
            }
            Thread.Sleep(random.Next(10000, 25000));
        }
    }

    public void SolicitaRecurso(Recurso recurso)
    {
        if (AguardandoRecurso)
            return;
        var requisicao = new Mensagem
        {
            Tipo = ETipoMensagem.Requisicao,
            RecursoId = recurso.Id,
            ProcessoId = Id
        };
        // envia requisicao para o coordenador
        AguardandoRecurso = true;
    }

    public void RecebeMensagem(Mensagem mensagem)
    {
        switch (mensagem.Tipo)
        {
            case ETipoMensagem.Requisicao:
                if (SouCoordenador)
                {
                    if (!Recurso.EmUso)
                    {
                        var concessao = new Mensagem
                        {
                            Tipo = ETipoMensagem.Concessao,
                            RecursoId = mensagem.RecursoId,
                            ProcessoId = mensagem.ProcessoId
                        };
                        // envia concessao
                        Recurso.EmUso = true;
                    }
                    else
                    {
                        FilaRequisicoes.Enqueue(mensagem);
                    }
                }
                else
                {
                    // perfume - responde que não é
                }
                    break;
            case ETipoMensagem.Concessao:
                if (AguardandoRecurso && mensagem.RecursoId == 1) // exemplo
                {
                    AguardandoRecurso = false;
                    var random = new Random(); // melhorar
                    var tempoUso = random.Next(5000, 15000);
                    Thread.Sleep(tempoUso);
                    var liberacao = new Mensagem
                    {
                        Tipo = ETipoMensagem.Liberacao,
                        RecursoId = mensagem.RecursoId
                    };
                    // envio pro coordenador
                }
                break;
            case ETipoMensagem.Liberacao:
                if (SouCoordenador)
                {
                    Recurso.EmUso = false;
                    // libera o recurso

                    // se tiver alguem na fila, concede
                    if (FilaRequisicoes.Count > 0)
                    {
                        var prox = FilaRequisicoes.Dequeue();
                        var concessao = new Mensagem
                        {
                            Tipo = ETipoMensagem.Concessao,
                            RecursoId = prox.RecursoId,
                            ProcessoId = prox.ProcessoId
                        };
                        // envia concessao
                        Recurso.EmUso = true;
                    }
                }
                break;
        }
    }
}

public class Recurso
{
    public long Id { get; set; }
    public string Nome { get; set; }
    public bool EmUso { get; set; }
    // qualquer outro atributo que seja necessário
}

public class Mensagem
{
    public ETipoMensagem Tipo { get; set; }
    public long RecursoId { get; set; }
    public int ProcessoId { get; set; }
}

public enum ETipoMensagem
{
    Requisicao,
    Concessao,
    Liberacao,
}