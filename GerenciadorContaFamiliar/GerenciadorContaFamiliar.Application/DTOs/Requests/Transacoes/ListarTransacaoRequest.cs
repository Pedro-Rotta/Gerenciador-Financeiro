using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;

public class ListarTransacaoRequest
{
    public int MembroId { get; set; }
    public int? ContaId { get; set; }
    public int? ContaConjuntaId { get; set; }
}