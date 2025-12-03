using FiveMotors.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;

namespace FiveMotors.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly FiveMotorsContext _context;

        public DashboardController(FiveMotorsContext context)
        {
            _context = context;
        }

        // ----------------------------------------------------------------------
        // GET: api/Dashboard
        // Retorna todos os dados consolidados para o painel administrativo
        // ----------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            // ==================================================================
            //                          TOTAIS GERAIS
            // ==================================================================
            var totalVendas = await _context.vendas.CountAsync();
            var totalVeiculos = await _context.veiculos.CountAsync();
            var totalMensagens = await _context.faleComVendedors.CountAsync();

            // Soma o faturamento total de todas as vendas
            var faturamentoTotal = await _context.vendas.SumAsync(v => v.Preco);


            // ==================================================================
            //                        VENDAS POR MÊS (últimos 6 meses)
            // ==================================================================
            var hoje = DateTime.Now;

            // Agrega vendas por ano/mês
            var vendasPorMesRaw = await _context.vendas
                .Where(v => v.DataHora >= hoje.AddMonths(-6))
                .GroupBy(v => new { v.DataHora.Year, v.DataHora.Month })
                .Select(g => new
                {
                    ano = g.Key.Year,
                    mes = g.Key.Month,
                    total = g.Sum(v => v.Preco)
                })
                .OrderBy(x => x.ano)
                .ThenBy(x => x.mes)
                .ToListAsync();

            // Formata o nome do mês corretamente em pt-BR
            var vendasPorMes = vendasPorMesRaw
                .Select(v => new
                {
                    mes = new DateTime(v.ano, v.mes, 1)
                        .ToString("MMMM yyyy", new CultureInfo("pt-BR")),
                    v.total
                })
                .ToList();


            // ==================================================================
            //                       VEÍCULOS MAIS VENDIDOS
            // ==================================================================
            var maisVendidos = await _context.vendas
                .GroupBy(v => v.VeiculoId)
                .Select(g => new
                {
                    veiculoId = g.Key,
                    quantidade = g.Count()
                })
                .OrderByDescending(x => x.quantidade)
                .Take(5)
                // Junta com tabela de veículos
                .Join(_context.veiculos,
                    v => v.veiculoId,
                    c => c.VeiculoId,
                    (v, carro) => new
                    {
                        carro.Marca,
                        carro.Modelo,
                        v.quantidade
                    })
                .ToListAsync();


            // ==================================================================
            //                         VENDAS POR STATUS
            // ==================================================================
            var vendasStatus = await _context.vendas
                .GroupBy(v => v.Status)
                .Select(g => new
                {
                    status = g.Key,
                    total = g.Count()
                })
                .ToListAsync();


            // ==================================================================
            //                    ÚLTIMOS VEÍCULOS CADASTRADOS
            // ==================================================================
            var ultimosVeiculos = (await _context.veiculos
                .OrderByDescending(v => v.VeiculoId)
                .Take(5)
                .ToListAsync())
                .Select(v => new
                {
                    v.Marca,
                    v.Modelo,
                    v.Preco,
                    v.Estoque,
                    // Retorna primeira imagem do veículo
                    Imagem = v.ImagemsVeiculo?.FirstOrDefault() ?? null
                })
                .ToList();


            // ==================================================================
            //                         ÚLTIMAS MENSAGENS
            // ==================================================================
            var ultimasMensagens = await _context.faleComVendedors
                .OrderByDescending(m => m.FaleComVendedorId)
                .Take(5)
                .ToListAsync();


            // ==================================================================
            //                             RETORNO FINAL
            // ==================================================================
            return Ok(new
            {
                totais = new
                {
                    vendas = totalVendas,
                    veiculos = totalVeiculos,
                    mensagens = totalMensagens,
                    faturamento = faturamentoTotal
                },
                vendasPorMes,
                maisVendidos,
                vendasStatus,
                ultimosVeiculos,
                ultimasMensagens
            });
        }
    }
}
