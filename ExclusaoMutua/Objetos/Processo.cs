using System.Net;
using System.Net.Sockets;

namespace ExclusaoMutua.Objetos;

public class Processo(int id, int coordenadorId, Recurso recurso)
{
    /// <summary>
    /// Padrão de mensagem para envio e recebimento entre processos.
    /// Formato: "{Tipo}_{ProcessoId}_{RecursoId}"
    /// </summary>
    const string PADRAO_MENSAGEM = "{0}_{1}_{2}";
    public int Id { get; set; } = id;
    public bool SouCoordenador { get; set; } = id == coordenadorId;
    public int Coordenador { get; set; } = coordenadorId;
    public bool AguardandoRecurso { get; set; }
    public Queue<Mensagem> FilaRequisicoes { get; set; } = new();
    public Recurso Recurso { get; set; } = recurso;
    public Thread? Requisitador { get; set; }

    public void IniciaProcesso(IPAddress ipAddress)
    {
        Console.WriteLine($"[Processo {Id}] Iniciando processo. Coordenador atual: {Coordenador}");
        if (!SouCoordenador)
        {
            Requisitador = new Thread(IniciaRequisitador);
            Requisitador.Start();
        }
        IniciaServidor(ipAddress);
    }

    public void IniciaServidor(IPAddress ipAddress)
    {
        TcpListener listener = new(ipAddress, 5000 + Id);
        listener.Start();
        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            RecebeRequisicao(client);
        }
    }

    public void RecebeCoordenador(int coordenadorId)
    {
        Coordenador = coordenadorId;
        if (Id == coordenadorId)
        {
            SouCoordenador = true;
            Requisitador?.Interrupt();
        }
        Console.WriteLine($"[Processo {Id}] Novo coordenador definido: {Coordenador}. Sou coordenador? {SouCoordenador}");
    }

    public void IniciaRequisitador()
    {
        var random = new Random();
        try
        {
            while (true)
            {
                SolicitaRecurso(Recurso);

                Thread.Sleep(random.Next(10000, 25000));
            }
        }
        catch (ThreadInterruptedException)
        {
            Console.WriteLine($"[Processo {Id}] Thread do requisitador foi interrompida.");
        }
    }

    public void SolicitaRecurso(Recurso recurso)
    {
        if (AguardandoRecurso || SouCoordenador)
            return;

        Console.WriteLine($"[Processo {Id}] Solicitando acesso ao recurso {recurso.Id} ({recurso.Nome}) ao coordenador {Coordenador}.");
        EnviaRequisicao();
        AguardandoRecurso = true;
    }

    public void EnviaRequisicao()
    {
        using TcpClient client = new();
        NetworkStream? stream = null;
        string serverIp = "127.0.0.1";
        int port = 5000 + Coordenador;
        try
        {
            client.Connect(serverIp, port);
            stream = client.GetStream();
            string payload = string.Format(PADRAO_MENSAGEM, ETipoMensagem.Requisicao, Id, Recurso.Id);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(payload);
            stream.Write(data, 0, data.Length);

            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string response = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (!string.IsNullOrEmpty(response) && response.Equals("available"))
            {
                Console.WriteLine($"[Processo {Id}] Recurso {Recurso.Id} ({Recurso.Nome}) disponível. Acesso concedido.");
                UtilizarRecurso();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Processo {Id}] Erro ao enviar requisição: {ex.Message}");
        }
        finally
        {
            stream?.Close();
        }
    }

    public void EnviaRequisicaoLiberacao()
    {
        using TcpClient client = new();
        NetworkStream? stream = null;
        string serverIp = "127.0.0.1";
        int port = 5000 + Coordenador;
        try
        {
            client.Connect(serverIp, port);
            stream = client.GetStream();
            string payload = string.Format(PADRAO_MENSAGEM, ETipoMensagem.Liberacao, Id, Recurso.Id);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(payload);
            stream.Write(data, 0, data.Length);
            Console.WriteLine($"[Processo {Id}] Enviou liberação do recurso {Recurso.Id} ao coordenador {Coordenador}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Processo {Id}] Erro ao enviar requisição de liberação: {ex.Message}");
        }
        finally
        {
            stream?.Close();
        }
    }

    public void EnviaRequisicaoConcessao(Mensagem proxima)
    {
        using TcpClient client = new();
        NetworkStream? stream = null;
        string serverIp = "127.0.0.1";
        int port = 5000 + proxima.ProcessoId;
        try
        {
            client.Connect(serverIp, port);
            stream = client.GetStream();
            string payload = string.Format(PADRAO_MENSAGEM, ETipoMensagem.Concessao, Id, Recurso.Id);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(payload);
            stream.Write(data, 0, data.Length);
            Console.WriteLine($"[Processo {Id}] Concessão enviada para processo {proxima.ProcessoId} (porta {port}): {payload}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Processo {Id}] Erro ao enviar concessão: {ex.Message}");
        }
        finally
        {
            stream?.Close();
        }
    }

    public void UtilizarRecurso()
    {
        Console.WriteLine($"[Processo {Id}] Acesso concedido ao recurso {Recurso.Id} ({Recurso.Nome}). Utilizando recurso...");
        var random = new Random();
        Thread.Sleep(random.Next(5000, 15000));
        Console.WriteLine($"[Processo {Id}] Liberação do recurso {Recurso.Id} ({Recurso.Nome}) após uso.");
        AguardandoRecurso = false;
        EnviaRequisicaoLiberacao();
    }

    public void RecebeRequisicao(TcpClient client)
    {
        try
        {
            using NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead > 0)
            {
                string message = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var partes = message.Split('_');
                if (partes.Length >= 2 && Enum.TryParse(partes[0], out ETipoMensagem tipoMensagem) && int.TryParse(partes[1], out int processoId))
                {
                    long? recursoId = null;
                    if (partes.Length == 3 && long.TryParse(partes[2], out long parsedRecursoId))
                        recursoId = parsedRecursoId;

                    var mensagem = new Mensagem(tipoMensagem, recursoId, processoId);

                    // retorna "available" na propria requisicao
                    if (tipoMensagem == ETipoMensagem.Requisicao && SouCoordenador && !Recurso.EmUso)
                    {
                        Recurso.EmUso = true;
                        stream.Write(System.Text.Encoding.UTF8.GetBytes("available"), 0, "available".Length);
                    }
                    else
                    {
                        ProcessaMensagem(mensagem);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Processo {Id}] Erro ao receber requisição: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    public void ProcessaMensagem(Mensagem mensagem)
    {
        switch (mensagem.Tipo)
        {
            case ETipoMensagem.Requisicao:
                if (SouCoordenador)
                {
                    if (!Recurso.EmUso)
                    {
                        Console.WriteLine($"[Processo {Id}] Recurso {Recurso.Id} disponível. Concedendo acesso ao processo {mensagem.ProcessoId}.");
                        var concessao = new Mensagem(ETipoMensagem.Concessao, mensagem.RecursoId, mensagem.ProcessoId);
                        // envia concessao
                        EnviaRequisicaoConcessao(concessao);
                        Recurso.EmUso = true;
                    }
                    else
                    {
                        if (!FilaRequisicoes.Any(x => x.ProcessoId == mensagem.ProcessoId))
                            FilaRequisicoes.Enqueue(mensagem);
                        Console.WriteLine($"[Processo {Id}] Recurso {Recurso.Id} em uso. Adicionando processo {mensagem.ProcessoId} à fila. Tamanho da fila: {FilaRequisicoes.Count}");
                    }
                }
                else
                {
                    Console.WriteLine($"[Processo {Id}] Não sou coordenador. Ignorando requisição.");
                }
                break;
            case ETipoMensagem.Concessao:
                if (AguardandoRecurso && mensagem.RecursoId == Recurso.Id)
                {
                    Console.WriteLine($"[Processo {Id}] Recebeu concessão para o recurso {mensagem.RecursoId}. Utilizando recurso.");
                    UtilizarRecurso();
                }
                break;
            case ETipoMensagem.Liberacao:
                if (SouCoordenador)
                {
                    Console.WriteLine($"[Processo {Id}] Recurso {Recurso.Id} foi liberado. Verificando fila de requisições.");
                    Recurso.EmUso = false;
                    if (FilaRequisicoes.Count > 0)
                    {
                        var prox = FilaRequisicoes.Dequeue();
                        Console.WriteLine($"[Processo {Id}] Concedendo recurso {Recurso.Id} ao próximo da fila: Processo {prox.ProcessoId}.");
                        Recurso.EmUso = true;
                        EnviaRequisicaoConcessao(prox);
                    }
                }
                break;
            case ETipoMensagem.NovoCoordenador:
                if (Coordenador != mensagem.ProcessoId)
                {
                    RecebeCoordenador(mensagem.ProcessoId);
                    AguardandoRecurso = false;
                    SolicitaRecurso(Recurso);
                }
                else
                {
                    FilaRequisicoes.Clear();
                    Console.WriteLine($"[Processo {Id}] Processo {Id} é o novo coordenador.");
                    SouCoordenador = true;
                }
                break;
        }
    }
}