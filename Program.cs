using Microsoft.EntityFrameworkCore;
using minimal_api.Dominio.Interfaces;
using minimal_api.Dominio.Servicos;
using minimal_api.Dominio.Entidades;
using minimal_api.Infraestrutura.Db;
using minimal_api.DTOs.Dominio;
using minimal_api.Dominio.ModelViews;
using Microsoft.AspNetCore.Mvc;
using minimal_api.Dominio.Enuns;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

#region Builder
  var builder = WebApplication.CreateBuilder(args);

    var connStr = builder.Configuration.GetConnectionString("MySql");
    var key = builder.Configuration.GetSection("Jwt")["Key"]?.ToString() ?? "123456";

    builder.Services.AddAuthentication(option => {
      option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
      option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(option => {
       option.TokenValidationParameters = new TokenValidationParameters{
         ValidateLifetime = true,
         IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
         ValidateIssuer = false,
         ValidateAudience = false 
       }; 
    });

    builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
    builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options => {
      options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme{
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira seu token JWT aqui"
      });


      options.AddSecurityRequirement(new OpenApiSecurityRequirement
      {
          {
              new OpenApiSecurityScheme
              {
                  Reference = new OpenApiReference
                  {
                      Type = ReferenceType.SecurityScheme,
                      Id = "Bearer"
                  }
              },
              new string[] {}
          }
      });



    });

    builder.Services.AddDbContext<DbContexto>(options => {
      options.UseMySql(
        connStr,
        ServerVersion.AutoDetect(connStr)
      );
    });

    var app = builder.Build();
#endregion

#region Home
  app.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
#endregion

#region Administradores

  string GerarTokenJwt(Administrador administrador)
  {
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>()
    {
      new Claim("Email", administrador.Email),
      new Claim("Perfil", administrador.Perfil),
      new Claim(ClaimTypes.Role, administrador.Perfil)
    };

    var token = new JwtSecurityToken(
      claims: claims,
      expires: DateTime.Now.AddDays(1),
      signingCredentials: credentials 
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

 ErrosDeValidacao validaAdmDTO(AdministradorDTO administradorDTO)
  {
    var validacao = new ErrosDeValidacao{
      Mensagens = new List<string>()
    };

    if(string.IsNullOrEmpty(administradorDTO.Email))
      validacao.Mensagens.Add("O EMAIL não pode ser vazio");

    if(string.IsNullOrEmpty(administradorDTO.Senha))
      validacao.Mensagens.Add("A SENHA não pode ser ficar em branco");  

    if(administradorDTO.Perfil == null)
      validacao.Mensagens.Add("O PERFIL ão pode ser vazio"); 

    
     return validacao; 
  }


  app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) => {
    var adm = administradorServico.Login(loginDTO); 
    if(adm != null)
    {
      string token = GerarTokenJwt(adm); 
      return Results.Ok("Login realizado com sucesso!");
    }
    else
      return Results.Unauthorized();
  }).AllowAnonymous().WithTags("Administradores");


  app.MapGet("/administradores", ([FromQuery] int pagina, IAdministradorServico administradorServico) => {

    var adms = new List<AdministradorModelView>();
    var administradores = administradorServico.Todos(pagina);
    foreach(var adm in administradores){
      adms.Add(new AdministradorModelView{
        Id = adm.Id,
        Email = adm.Email,
        Perfil = adm.Perfil
      });
    }

    return Results.Ok(adms);
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
.WithTags("Administradores");


   app.MapGet("/administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) => {
    var administrador = administradorServico.BuscarPorId(id);
    if (administrador == null) return Results.NotFound();
    return Results.Ok(new AdministradorModelView{
      Id = administrador.Id,
      Email = administrador.Email,
      Perfil =administrador.Perfil  
    });
  }).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
  . WithTags("Administradores");


 app.MapPost("/administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) => {
    var validacao = validaAdmDTO(administradorDTO);  
    if(validacao.Mensagens.Count > 0)
      return Results.BadRequest();

    var administrador = new Administrador{
      Email = administradorDTO.Email,
      Senha = administradorDTO.Senha,
      Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
    };
    administradorServico.Incluir(administrador);
    return Results.Created($"/administrador/{administrador.Id}", new AdministradorModelView{
      Id = administrador.Id,
      Email = administrador.Email,
      Perfil = administrador.Perfil
    });

    
  }).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
  .WithTags("Administradores");

#endregion

#region Veiculos

 ErrosDeValidacao validaVeiculoDTO(VeiculoDTO veiculoDTO)
  {
    var validacao = new ErrosDeValidacao{
      Mensagens = new List<string>()
    };

    if(string.IsNullOrEmpty(veiculoDTO.Nome))
      validacao.Mensagens.Add("O NOME não pode ser vazio");

    if(string.IsNullOrEmpty(veiculoDTO.Marca))
      validacao.Mensagens.Add("A MARCA não pode ser ficar em branco");  

    if(string.IsNullOrEmpty(veiculoDTO.Marca))
      validacao.Mensagens.Add("A MARCA não pode ser ficar em branco"); 

    if(veiculoDTO.Ano < 1950)
      validacao.Mensagens.Add("Veículo muito antigo. Só serão aceitos veículos à parteir de 1951"); 

     return validacao; 
  }


  app.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) => {

    var validacao = validaVeiculoDTO(veiculoDTO);  
    if(validacao.Mensagens.Count > 0)
      return Results.BadRequest();

    var veiculo = new Veiculo{
      Nome = veiculoDTO.Nome,
      Marca = veiculoDTO.Marca,
      Ano = veiculoDTO.Ano
    };
    veiculoServico.Incluir(veiculo);
    return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
  }).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm,Edior"})
  .WithTags("Veiculos");

  app.MapGet("/veiculos", ([FromQuery] int pagina, IVeiculoServico veiculoServico) => {
    var veiculos = veiculoServico.Todos(pagina);
    
    return Results.Ok(veiculos);
  }).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm,Edior"})
  .WithTags("Veiculos");

  app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) => {
    var veiculo = veiculoServico.BuscarPorId(id);
    if (veiculo == null) return Results.NotFound();
    return Results.Ok(veiculo);
  }).RequireAuthorization().WithTags("Veiculos");

  app.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) => {
    var veiculo = veiculoServico.BuscarPorId(id);
    if (veiculo == null) return Results.NotFound();

    var validacao = validaVeiculoDTO(veiculoDTO);  
    if(validacao.Mensagens.Count > 0)
      return Results.BadRequest();

    veiculo.Nome = veiculoDTO.Nome;
    veiculo.Marca = veiculoDTO.Marca;
    veiculo.Ano = veiculoDTO.Ano;

    veiculoServico.Atualizar(veiculo);

    return Results.Ok(veiculo);
  }).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
  .WithTags("Veiculos");

  app.MapDelete("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) => {
    var veiculo = veiculoServico.BuscarPorId(id);
    if (veiculo == null) return Results.NotFound();

    veiculoServico.Apagar(veiculo);

    return Results.NoContent();
  }).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
  .WithTags("Veiculos");
#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

#endregion
