using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Servicos;
using minimal_api.DTOs.Dominio;

namespace minimal_api.Dominio.Interfaces
{
    public interface IAdministradorServico
    {
        Administrador? Login(LoginDTO loginDTO);
        Administrador Incluir(Administrador administrador);
        Administrador? BuscarPorId(int id);            
        List<Administrador> Todos(int pagina);
    }    
}