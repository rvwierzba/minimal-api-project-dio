using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace minimal_api.Dominio.ModelViews
{
    public struct Home
    {

         public string Mensagem { get => "Seja bem vindo a Minimal API de Veículos"; }
        public string Doc { get => "/swagger"; }
    }
}