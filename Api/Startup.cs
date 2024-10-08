using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Interfaces;
using minimal_api.Dominio.ModelViews;
using minimal_api.Dominio.Servicos;
using minimal_api.DTOs.Dominio;
using minimal_api.Infraestrutura.Db;
using Microsoft.AspNetCore.Mvc;
using minimal_api.Dominio.Enuns;

namespace minimal_api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            key = Configuration["Jwt"] ?? "";
        }

        public IConfiguration Configuration { get; }
        private readonly string key;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            services.AddAuthorization();

            services.AddScoped<IAdministradorServico, AdministradorServico>();
            services.AddScoped<IVeiculoServico, VeiculoServico>();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
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

            var connStr = Configuration.GetConnectionString("MySql");

            services.AddDbContext<DbContexto>(options =>
            {
                options.UseMySql(
                    connStr,
                    ServerVersion.AutoDetect(connStr)
                );
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                #region Home
                endpoints.MapGet("/", () => Results.Json(new Home()))
                    .AllowAnonymous()
                    .WithTags("Home");
                #endregion

                #region Administradores

                string GerarTokenJwt(Administrador administrador)
                {
                    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                    var claims = new List<Claim>
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

                ErrosDeValidacao ValidaAdmDTO(AdministradorDTO administradorDTO)
                {
                    var validacao = new ErrosDeValidacao
                    {
                        Mensagens = new List<string>()
                    };

                    if (string.IsNullOrEmpty(administradorDTO.Email))
                        validacao.Mensagens.Add("O EMAIL não pode ser vazio");

                    if (string.IsNullOrEmpty(administradorDTO.Senha))
                        validacao.Mensagens.Add("A SENHA não pode ficar em branco");

                    if (administradorDTO.Perfil == null)
                        validacao.Mensagens.Add("O PERFIL não pode ser vazio");

                    return validacao;
                }

                endpoints.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
                {
                    var adm = administradorServico.Login(loginDTO);
                    if (adm != null)
                    {
                        string token = GerarTokenJwt(adm);
                        return Results.Ok(new { token });
                    }
                    else
                        return Results.Unauthorized();
                })
                .AllowAnonymous()
                .WithTags("Administradores");

                endpoints.MapGet("/administradores", ([FromQuery] int pagina, IAdministradorServico administradorServico) =>
                {
                    var adms = new List<AdministradorModelView>();
                    var administradores = administradorServico.Todos(pagina);
                    foreach (var adm in administradores)
                    {
                        adms.Add(new AdministradorModelView
                        {
                            Id = adm.Id,
                            Email = adm.Email,
                            Perfil = adm.Perfil
                        });
                    }

                    return Results.Ok(adms);
                })
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
                .WithTags("Administradores");

                endpoints.MapGet("/administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) =>
                {
                    var administrador = administradorServico.BuscarPorId(id);
                    if (administrador == null) return Results.NotFound();
                    return Results.Ok(new AdministradorModelView
                    {
                        Id = administrador.Id,
                        Email = administrador.Email,
                        Perfil = administrador.Perfil
                    });
                })
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
                .WithTags("Administradores");

                endpoints.MapPost("/administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
                {
                    var validacao = ValidaAdmDTO(administradorDTO);
                    if (validacao.Mensagens.Count > 0)
                        return Results.BadRequest(validacao);

                    var administrador = new Administrador
                    {
                        Email = administradorDTO.Email,
                        Senha = administradorDTO.Senha,
                        Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
                    };
                    administradorServico.Incluir(administrador);
                    return Results.Created($"/administrador/{administrador.Id}", new AdministradorModelView
                    {
                        Id = administrador.Id,
                        Email = administrador.Email,
                        Perfil = administrador.Perfil
                    });
                })
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
                .WithTags("Administradores");

                #endregion

                #region Veiculos

                ErrosDeValidacao ValidaVeiculoDTO(VeiculoDTO veiculoDTO)
                {
                    var validacao = new ErrosDeValidacao
                    {
                        Mensagens = new List<string>()
                    };

                    if (string.IsNullOrEmpty(veiculoDTO.Nome))
                        validacao.Mensagens.Add("O NOME não pode ser vazio");

                    if (string.IsNullOrEmpty(veiculoDTO.Marca))
                        validacao.Mensagens.Add("A MARCA não pode ficar em branco");

                    if (veiculoDTO.Ano < 1950)
                        validacao.Mensagens.Add("Veículo muito antigo. Só serão aceitos veículos a partir de 1951");

                    return validacao;
                }

                endpoints.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
                {
                    var validacao = ValidaVeiculoDTO(veiculoDTO);
                    if (validacao.Mensagens.Count > 0)
                        return Results.BadRequest(validacao);

                    var veiculo = new Veiculo
                    {
                        Nome = veiculoDTO.Nome,
                        Marca = veiculoDTO.Marca,
                        Ano = veiculoDTO.Ano
                    };
                    veiculoServico.Incluir(veiculo);
                    return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
                })
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm,Editor" })
                .WithTags("Veiculos");

                endpoints.MapGet("/veiculos", ([FromQuery] int pagina, IVeiculoServico veiculoServico) =>
                {
                    var veiculos = veiculoServico.Todos(pagina);

                    return Results.Ok(veiculos);
                })
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm,Editor" })
                .WithTags("Veiculos");

                endpoints.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
                {
                    var veiculo = veiculoServico.BuscarPorId(id);
                    if (veiculo == null) return Results.NotFound();
                    return Results.Ok(veiculo);
                })
                .RequireAuthorization()
                .WithTags("Veiculos");

                endpoints.MapPut("/veiculos/{id}", ([FromRoute] int id, [FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
                {
                    var veiculo = veiculoServico.BuscarPorId(id);
                    if (veiculo == null) return Results.NotFound();

                    var validacao = ValidaVeiculoDTO(veiculoDTO);
                    if (validacao.Mensagens.Count > 0)
                        return Results.BadRequest(validacao);

                    veiculo.Nome = veiculoDTO.Nome;
                    veiculo.Marca = veiculoDTO.Marca;
                    veiculo.Ano = veiculoDTO.Ano;

                    veiculoServico.Atualizar(veiculo);

                    return Results.Ok(veiculo);
                })
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
                .WithTags("Veiculos");

                endpoints.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
                {
                    var veiculo = veiculoServico.BuscarPorId(id);
                    if (veiculo == null) return Results.NotFound();

                    veiculoServico.Apagar(veiculo);

                    return Results.NoContent();
                })
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
                .WithTags("Veiculos");

                #endregion
            });
        }
    }
}
