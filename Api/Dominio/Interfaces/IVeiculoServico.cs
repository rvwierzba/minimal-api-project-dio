using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Servicos;
using minimal_api.DTOs.Dominio;

namespace minimal_api.Dominio.Interfaces
{
    public interface IVeiculoServico
    {
        List<Veiculo> Todos(int pagina = 1, string? nome = null, string? marca = null);
        Veiculo? BuscarPorId(int id);
        void Incluir(Veiculo veiculo);
        void Atualizar(Veiculo veiculo);
        void Apagar(Veiculo veiculo);
    }    
}