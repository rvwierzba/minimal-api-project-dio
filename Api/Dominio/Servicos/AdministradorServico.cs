using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Interfaces;
using minimal_api.DTOs.Dominio;
using minimal_api.Infraestrutura.Db;

namespace minimal_api.Dominio.Servicos
{
    public class AdministradorServico : IAdministradorServico
    {
        private readonly DbContexto _contexto;
        public AdministradorServico(DbContexto contexto)
        {
          _contexto = contexto;  
        }

        public Administrador? BuscarPorId(int id)
        {
            return _contexto.Administradores.Where(A => A.Id == id).FirstOrDefault();
        }

        public Administrador Incluir(Administrador administrador)
        {
            _contexto.Administradores.Add(administrador);
            _contexto.SaveChanges();

            return administrador;
        }

        public Administrador? Login(LoginDTO loginDTO)
        {
            var adm =_contexto.Administradores.Where(a => a.Email == loginDTO.Email && a.Senha == loginDTO.Senha).FirstOrDefault();
            return adm;
        }

        public List<Administrador> Todos(int pagina)
        {
            var query = _contexto.Administradores.AsQueryable();
            int itensPorPagina = 10;

            query = query.Skip((pagina - 1) * itensPorPagina).Take(itensPorPagina);


            return query.ToList();
        }
    }
}