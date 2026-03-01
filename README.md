# Password Manager (.NET MAUI Blazor + Minimal API)

Aplicação completa para **gestão de senhas** baseada em uma arquitetura semelhante ao padrão de separação em camadas do `finance-app`, com:

- **Backend** em **ASP.NET Core Minimal API**.
- **Autenticação JWT (login)**.
- **Swagger** protegido por usuário/senha (Basic Auth) e suporte a Bearer token.
- **Frontend** em **.NET MAUI Blazor** com **MudBlazor**.
- Fluxo de **geração automática de senha** e vínculo com descrição/login.

---

## Arquitetura da solução

```text
PasswordManager.sln
└── src
    ├── PasswordManager.Api        # Minimal API + Auth + Swagger
    │   ├── Infrastructure         # Store de usuários (seed de login)
    │   ├── Models                 # Requests e entidades
    │   ├── Security               # Configuração e emissão de JWT
    │   └── Services               # Repositório em memória + gerador de senha
    └── PasswordManager.App        # .NET MAUI Blazor + MudBlazor
        ├── Layout                 # Layout principal
        ├── Models                 # Contratos para consumo da API
        ├── Pages                  # Tela de login e gestão de senhas
        ├── Services               # Cliente HTTP e estado de autenticação
        └── wwwroot                # Host webview e estilos
```

---

## Funcionalidades implementadas

### Backend (Minimal API)

- `POST /api/auth/register`
  - Cria um novo usuário em memória para autenticação na API.
- `POST /api/auth/login`
  - Autentica usuário e retorna token JWT.
- `GET /api/passwords/` (protegido)
  - Lista as senhas cadastradas.
- `POST /api/passwords/generate` (protegido)
  - Gera senha automática com parâmetros (tamanho e grupos de caracteres).
- `POST /api/passwords/` (protegido)
  - Cria nova senha vinculando com descrição e usuário/login.
  - Se `password` não for informada, gera automaticamente.

### Frontend (MAUI Blazor + MudBlazor)

- Tela de login.
- Tela de cadastro de senha com:
  - descrição,
  - login/usuário,
  - senha manual ou automática.
- Listagem das senhas já cadastradas.
- Interface com componentes MudBlazor (AppBar, Paper, Table, Buttons, Alerts).

---

## Credenciais para testes

Não há usuários padrão criados por migration. Crie um usuário via `POST /api/auth/register` antes de autenticar.

Credenciais de acesso ao Swagger (Basic Auth), definidas em `appsettings.json`:

- `swagger / Swagger@123`

> Recomenda-se mover para um provedor seguro (Identity/DB + hashing) em produção.

---

## Como executar

> Pré-requisitos: .NET 8 SDK com workload MAUI instalado.

### 1) API

```bash
cd src/PasswordManager.Api
dotnet restore
dotnet run
```

A API sobe em:

- `http://localhost:5096`
- `https://localhost:7096`

Swagger:

- `https://localhost:7096/swagger`


### 1.1) Observabilidade com Serilog + Seq

A API está configurada para enviar logs estruturados para o console e para o Seq.

Instale/execute o Seq localmente (sem container) no endereço padrão (`http://localhost:5341`).

A API também expõe um proxy para o Seq em:

- `http://localhost:5096/seq`
- `https://localhost:7096/seq`

> Caso seu Seq rode em outro endereço, ajuste `Seq:ServerUrl` na configuração da API.

### 2) App MAUI Blazor

Em outro terminal:

```bash
cd src/PasswordManager.App
dotnet restore
dotnet build
```

Para execução no Windows, por exemplo:

```bash
dotnet build -t:Run -f net8.0-windows10.0.19041.0
```

> O `ApiClient` está apontando para `http://localhost:5096`. Ajuste para ambiente/dispositivo real se necessário.

---

## Boas práticas e próximos passos (produção)

- Persistência com banco (SQL Server/PostgreSQL) e EF Core.
- Hash de senhas de acesso dos usuários (Identity + PBKDF2/Argon2).
- Criptografia dos segredos (senhas armazenadas) em repouso.
- Controle de autorização por usuário/tenant.
- Rotação e externalização de chave JWT (Azure Key Vault/Secrets Manager).
- Testes automatizados (unitários + integração).
- Pipeline CI/CD com validações de segurança.

---

## Observações

Este projeto foi estruturado para ser um ponto de partida profissional e evolutivo, mantendo separação clara entre API e app cliente, com foco em produtividade no front (MudBlazor) e simplicidade no backend (Minimal API).
