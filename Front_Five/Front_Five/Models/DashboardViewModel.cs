namespace FiveMotors.Models
{
    public class DashboardViewModel
    {
        public Totais Totais { get; set; }
        public List<VendaPorMes> VendasPorMes { get; set; } 
        public List<MaisVendido> MaisVendidos { get; set; } 
        public List<VendaStatus> VendasStatus { get; set; } 
        public List<UltimoVeiculo> UltimosVeiculos { get; set; } 
        public List<UltimaMensagem> UltimasMensagens { get; set; } 
    }
}
