using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Interfaces;
using minimal_api.DTOs.Dominio;
using minimal_api.Infraestrutura.Db;

namespace minimal_api.Dominio.Servicos
{
    public class VeiculoServico : IVeiculoServico
    {
        private readonly DbContexto _contexto;
        public VeiculoServico(DbContexto contexto)
        {
          _contexto = contexto;  
        }

        public void Apagar(Veiculo veiculo)
        {
            _contexto.Veiculos.Remove(veiculo);
            _contexto.SaveChanges();
        }

        public void Atualizar(Veiculo veiculo)
        {
           _contexto.Veiculos.Update(veiculo);
           _contexto.SaveChanges();
        }

        public Veiculo? BuscarPorId(int id)
        {
            return _contexto.Veiculos.Where(V => V.Id == id).FirstOrDefault();
        }

        public void Incluir(Veiculo veiculo)
        {
            _contexto.Veiculos.Add(veiculo);
            _contexto.SaveChanges();
        }

        public List<Veiculo> Todos(int pagina = 1, string? nome = null, string? marca = null)
        {
            var query = _contexto.Veiculos.AsQueryable();
            if(!string.IsNullOrWhiteSpace(nome))
            {
                query = query.Where(v => EF.Functions.Like(v.Nome.ToLower(), $"%{nome}%"));
            }

            int itensPorPagina = 10;

            query = query.Skip((pagina - 1) * itensPorPagina).Take(itensPorPagina);


            return query.ToList();
        }
    }
      
}