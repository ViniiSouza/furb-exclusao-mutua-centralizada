using ExclusaoMutua.Objetos;
using System.Net;
using System.Net.Sockets;

namespace ExclusaoMutua;

public class Contexto
{
    /// <summary>
    /// Padrão de mensagem para envio e recebimento entre processos.
    /// Formato: "{Tipo}_{ProcessoId}_{RecursoId}"
    /// </summary>
    const string PADRAO_MENSAGEM = "{0}_{1}_{2}";
    public IPAddress IpAddress { get; set; }
    public Dictionary<int, Thread> Processos { get; set; }
    public int CoordenadorId { get; set; }
    public Recurso Recurso { get; set; }

    public Contexto()
    {
        Console.WriteLine("[Contexto] Inicializando contexto...");
        IpAddress = IPAddress.Parse("127.0.0.1");
        Recurso = new Recurso
        {
            Id = 1,
            Nome = "Recurso 1"
        };
        CoordenadorId = 1;
        Processos = [];
        new Thread(CriaProcessos).Start();
        new Thread(MataCoordenador).Start();
        Console.WriteLine("[Contexto] Thread de criação de processos iniciada.");
    }

    public void CriaProcessos()
    {
        int contador = 1;
        while (true)
        {
            Console.WriteLine($"[Contexto] Criando processo {contador}...");
            var processo = new Processo(contador, CoordenadorId, Recurso);
            var thread = new Thread(() => processo.IniciaProcesso(IpAddress));
            Processos.Add(contador, thread);
            thread.Start();
            Console.WriteLine($"[Contexto] Processo {contador} iniciado.");
            contador++;
            Thread.Sleep(40000);
        }
    }

    public void DefineCoordenador()
    {
        Console.WriteLine("[Contexto] Definindo novo coordenador...");
        var coordenadorId = Processos
            .OrderBy(x => Guid.NewGuid())
            .FirstOrDefault().Key;

        CoordenadorId = coordenadorId;
        Console.WriteLine($"[Contexto] Novo coordenador definido: {CoordenadorId}");
        ComunicaNovoCoordenador();
    }

    public void ComunicaNovoCoordenador()
    {
        Console.WriteLine("[Contexto] Comunicando novo coordenador para todos os processos...");
        foreach (var processo in Processos)
        {
            try
            {
                using TcpClient client = new();
                client.Connect(IpAddress, 5000 + processo.Key);
                using NetworkStream stream = client.GetStream();
                var mensagem = string.Format(PADRAO_MENSAGEM, ETipoMensagem.NovoCoordenador, CoordenadorId, 0);
                var data = System.Text.Encoding.UTF8.GetBytes(mensagem);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contexto] Erro ao comunicar novo coordenador ao processo {processo.Key}: {ex.Message}");
            }
        }
    }

    public void MataCoordenador()
    {
        Console.WriteLine("[Contexto] Aguardando para matar coordenador...");
        while (true)
        {
            Thread.Sleep(60000);
            if (Processos.TryGetValue(CoordenadorId, out Thread? coordenadorThread))
            {
                if (coordenadorThread.IsAlive)
                {
                    Console.WriteLine($"[Contexto] Abortando thread do coordenador {CoordenadorId}.");
                    coordenadorThread.Interrupt();
                }
                Processos.Remove(CoordenadorId);
                Console.WriteLine($"[Contexto] Coordenador {CoordenadorId} removido.");
            }
            DefineCoordenador();
        }
    }
}
