using System.Net;
using System.Net.Sockets;

namespace ExclusaoMutua
{
    public class Contexto
    {
        public IPAddress IpAddress { get; set; }
        //public List<Processo> Processos { get; set; } = new();

        public Dictionary<int, Thread> Processos { get; set; }
        public int CoordenadorId { get; set; }
        public Recurso Recurso { get; set; }

        public Contexto()
        {
            IpAddress = IPAddress.Parse("127.0.0.1");
            Recurso recurso = new Recurso
            {
                Id = 1,
                Nome = "Recurso 1"
            };
            // cria uma thread
            CoordenadorId = 1;
            Thread criadorProcessos = new Thread(CriaProcessos);
        }

        public void CriaProcessos()
        {
            var contador = 1;

            while (true)
            {
                var processo = new Processo(contador, CoordenadorId, Recurso);
                Thread thread = new Thread(() => processo.IniciaServidor(IpAddress));
                Processos.Add(contador, thread);
                contador++;
                Thread.Sleep(40000);
            }
        }

        public void DefineCoordenador()
        {
            var coordenadorId = Processos
                .OrderBy(x => Guid.NewGuid())
                .FirstOrDefault();

            
            foreach (var processo in Processos)
            {
               // chamar processos passando coordenadorId
            }
        }

        public void MataCoordenador()
        {
            Thread.Sleep(60000);
            if (Processos.ContainsKey(CoordenadorId))
            {
                var coordenadorThread = Processos[CoordenadorId];
                if (coordenadorThread.IsAlive)
                {
                    coordenadorThread.Abort(); // Aborta a thread do coordenador
                }
                Processos.Remove(CoordenadorId);
            }
            DefineCoordenador();
        }
    }
}
