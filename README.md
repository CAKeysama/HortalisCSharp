# HortalisCSharp

[![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)](#)
[![Coverage](https://img.shields.io/badge/coverage-0%25-red.svg)](#)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)

Resumo
-----
Aplicação web desenvolvida em .NET 8 (Razor Pages) para o gerenciamento de hortas urbanas comunitárias, com funcionalidades de cadastro, geolocalização, relatórios e acompanhamento de indicadores ambientais e sociais. O sistema é voltado para prefeituras, operadores municipais e administradores locais, auxiliando na gestão e expansão de hortas públicas como ferramenta de inclusão social, sustentabilidade e segurança alimentar.

Sumário
-------
- [Contexto e Propósito](#contexto-e-propósito)
- [Funcionalidades Principais](#funcionalidades-principais)
- [Tecnologias](#tecnologias)
- [Arquitetura](#arquitetura)
- [Estrutura de Diretórios](#estrutura-de-diretórios)
- [Instalação e Execução](#instalacao-e-execucao)
- [Configuração / Variáveis de ambiente](#configuracao--variaveis-de-ambiente)
- [Uso / Exemplos](#uso--exemplos)
- [Testes e Qualidade](#testes-e-qualidade)
- [Contribuição](#contribuicao)
- [Licença](#licenca)
- [Créditos](#creditos)

Contexto e Propósito
--------------------
O Hortalis foi criado para resolver um desafio enfrentado por muitas cidades: mapear e gerenciar hortas urbanas e indicações de novas áreas com base em critérios de viabilidade e demanda comunitária.

Ele centraliza informações sobre hortas existentes, permite o envio de indicações geográficas e fornece relatórios administrativos sobre o desenvolvimento urbano verde.

Audiência principal:

- Prefeituras e secretarias de meio ambiente

- Operadores do sistema (gestores locais)

- Cidadãos interessados em indicar áreas verdes

Funcionalidades Principais
------------------------

- 📍 **Cadastro de hortas** e indicações com geolocalização via *Leaflet* + *OpenStreetMap*  
- 🗺️ **Mapa interativo** para visualização e agrupamento de hortas  
- 📊 **Painel administrativo** com relatórios resumidos *(hortas, produtos, usuários ativos)*  
- 🔒 **Sistema de login e perfis de usuário** *(Administrador / Operador)*  
- 🌾 **Cadastro de produtos cultivados** e disponibilidade nas hortas  
- 🧾 **Geração de relatórios** para tomada de decisão  
- 🔄 **API REST** para integração de dados *(produtos e hortas)*

Tecnologias
-----------
-Backend: .NET 8 / C# 12 (Razor Pages + Controllers auxiliares)
-ORM: Entity Framework Core (migrations, LINQ, Data Annotations)
-Banco de Dados: SQL Server
-Frontend: Bootstrap 5, Leaflet.js, SweetAlert2
-APIs: OpenStreetMap / Nominatim (para reverse geocoding)
-Serialização: System.Text.Json

Arquitetura
----------
A aplicação segue o padrão MVC simplificado através do modelo Razor Pages, com Controllers auxiliares para APIs e áreas administrativas.

Fluxo resumido:

-O usuário acessa a aplicação web.
-O sistema consulta e exibe dados via EF Core.
-A geolocalização é renderizada via Leaflet no frontend.
-O painel administrativo consolida estatísticas e relatórios.

Estrutura de diretórios
-----------------------
- HortalisCSharp/ ou raiz do projeto: código da aplicação (Pages/, Controllers/, Views/, wwwroot/)
- Migrations/: migrations EF Core
- Models/: entidades e viewmodels
- Data/: AppDbContext
- Pages/: Razor Pages (Create/Edit/Index)
- wwwroot/: assets (js, css)

Instalação e Execução
---------------------
Pré-requisitos:
- .NET 8 SDK instalado
- SQL Server ou outro banco compatível com EF Core
- Node/npm apenas para ferramentas front (opcional)

Comandos (exatos):
1. Restaurar dependências:
   - __dotnet restore__
2. Build:
   - __dotnet build__
3. Aplicar migrations e criar banco:
   - __dotnet ef database update__
4. Executar:
   - __dotnet run__
Acesse: http://localhost:5000 (ou porta indicada no output)

Configuração / Variáveis de ambiente
-----------------------------------
- Connection string: `ConnectionStrings:DefaultConnection` (appsettings) — configure no ambiente ou em __appsettings.json__.
- ASPNETCORE_ENVIRONMENT: Development/Production


Licença
-------
Este projeto é licenciado sob os termos da licença **MIT**.  
Consulte o arquivo [LICENSE.md](LICENSE.md) para mais detalhes.

Créditos e Reconhecimentos
--------------------------
- Autores do projeto
- Bibliotecas de terceiros (EF Core, Leaflet, Bootstrap, etc.)
- Serviços externos: OpenStreetMap

Contato
-------
Para dúvidas técnicas abra uma issue no repositório ou envie PR com correção.