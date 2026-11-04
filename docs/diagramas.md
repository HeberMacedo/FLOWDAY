# Diagramas do FlowDay

Os diagramas abaixo usam [Mermaid](https://mermaid.js.org/) e são desenhados automaticamente pelo GitHub. As versões em imagem estão em [`docs/imagens/`](imagens/) e no documento de evidências.

Os diagramas de casos de uso, de contexto e o modelo conceitual de dados também aparecem no [ERS](ERS_FlowDay.pdf). Os fluxos de processo (atual e futuro) estão no [BRD](BRD_FlowDay.pdf).

Sumário:

1. [Diagrama de casos de uso](#1-diagrama-de-casos-de-uso)
2. [Arquitetura em camadas](#2-arquitetura-em-camadas)
3. [Diagrama de implantação (Docker)](#3-diagrama-de-implantação-docker)
4. [Diagrama de classes: domínio](#4-diagrama-de-classes-domínio)
5. [Diagrama de classes: aplicação e infraestrutura](#5-diagrama-de-classes-aplicação-e-infraestrutura)
6. [Diagrama entidade-relacionamento](#6-diagrama-entidade-relacionamento)
7. [Diagrama de estados da tarefa](#7-diagrama-de-estados-da-tarefa)
8. [Sequência: login](#8-diagrama-de-sequência-login)
9. [Sequência: concluir tarefa](#9-diagrama-de-sequência-concluir-tarefa)
10. [Sequência: recuperação de senha](#10-diagrama-de-sequência-recuperação-de-senha)
11. [Sequência: excluir área](#11-diagrama-de-sequência-excluir-área)

---

## 1. Diagrama de casos de uso

```mermaid
flowchart LR
    V(["👤 Visitante"])
    U(["👤 Usuário autenticado"])
    E[["✉️ Servidor de e-mail"]]

    subgraph S["Sistema FlowDay"]
        direction TB
        UC01(["UC01 Cadastrar conta"])
        UC02(["UC02 Fazer login e logout"])
        UC03(["UC03 Recuperar senha"])
        UC04(["UC04 Gerenciar tarefas"])
        UC05(["UC05 Gerenciar checklist"])
        UC06(["UC06 Gerenciar áreas"])
        UC07(["UC07 Visualizar em lista, Kanban e calendário"])
        UC08(["UC08 Buscar e filtrar tarefas"])
        UC09(["UC09 Consultar painel"])
        UC10(["UC10 Gerenciar perfil e conta"])
    end

    V --- UC01
    V --- UC02
    V --- UC03
    U --- UC02
    U --- UC04
    U --- UC05
    U --- UC06
    U --- UC07
    U --- UC08
    U --- UC09
    U --- UC10
    UC03 --- E
    UC05 -. "«extend»" .-> UC04
    UC08 -. "«extend»" .-> UC07
```

## 2. Arquitetura em camadas

As dependências apontam sempre para o domínio (arquitetura limpa). O domínio não conhece banco, HTTP nem frameworks.

```mermaid
flowchart TB
    subgraph Cliente["Front-end (HTML, CSS e JavaScript)"]
        FE["Telas: login, cadastro, painel,<br/>lista, Kanban, calendário, áreas e perfil"]
    end

    subgraph API["FlowDay.Api"]
        C["Controllers REST"]
        M["JWT · Rate limiting · CORS<br/>Tratador de exceções · Swagger"]
    end

    subgraph APP["FlowDay.Application"]
        S["Serviços (casos de uso)"]
        D["DTOs de entrada e saída"]
        P["Interfaces (portas):<br/>repositórios, hash, token, e-mail, relógio"]
    end

    subgraph DOM["FlowDay.Domain"]
        ENT["Entidades e regras de negócio<br/>Usuario · Area · Tarefa · ItemChecklist · RecuperacaoSenha"]
    end

    subgraph INF["FlowDay.Infrastructure"]
        R["Repositórios EF Core"]
        X["BCrypt · SMTP · Relógio (America/Sao_Paulo)"]
    end

    DB[("MySQL 8.4")]
    MAIL[["Mailpit (SMTP)"]]

    FE -- "HTTP + JSON<br/>Bearer token" --> C
    C --> M
    C --> S
    S --> D
    S --> P
    S --> ENT
    R -. "implementa" .-> P
    X -. "implementa" .-> P
    R --> ENT
    R --> DB
    X --> MAIL
```

## 3. Diagrama de implantação (Docker)

```mermaid
flowchart LR
    subgraph Host["Computador (Docker Compose)"]
        direction LR
        WEB["flowday-web<br/>nginx 1.27<br/>:8081"]
        API["flowday-api<br/>ASP.NET Core 8<br/>:8080"]
        DB[("flowday-mysql<br/>MySQL 8.4<br/>:3307")]
        MAIL[["flowday-mailpit<br/>SMTP :1025<br/>Web :8025"]]
        VOL[/"volume flowday-dados"/]
    end
    NAV["Navegador<br/>(computador ou celular)"]

    NAV -- "páginas" --> WEB
    NAV -- "REST /api (CORS)" --> API
    NAV -- "Swagger /swagger" --> API
    API -- "EF Core (Pomelo)" --> DB
    API -- "SMTP" --> MAIL
    DB --- VOL
```

## 4. Diagrama de classes: domínio

```mermaid
classDiagram
    direction LR

    class Usuario {
        +Guid Id
        +string Nome
        +string Email
        +string EmailNormalizado
        +string SenhaHash
        +int FalhasLogin
        +DateTime? BloqueadoAte
        +DateTime SenhaAlteradaEm
        +DateTime CriadoEm
        +long CarimboSeguranca
        +AlterarNome(nome)
        +AlterarEmail(email)
        +AlterarSenha(novoHash, agora)
        +EstaBloqueado(agora) bool
        +RegistrarFalhaLogin(agora)
        +RegistrarLoginComSucesso()
    }

    class Area {
        +Guid Id
        +Guid UsuarioId
        +string Nome
        +string NomeNormalizado
        +bool EhGeral
        +CriarPadrao(usuarioId)$ List~Area~
        +Renomear(nome)
        +GarantirQuePodeSerExcluida()
    }

    class Tarefa {
        +Guid Id
        +Guid UsuarioId
        +Guid AreaId
        +string Titulo
        +string? Descricao
        +StatusTarefa Status
        +Prioridade Prioridade
        +DateOnly? Prazo
        +DateTime? ConcluidaEm
        +DateTime CriadaEm
        +DateTime AtualizadaEm
        +Atualizar(...)
        +AlterarStatus(novoStatus, agora)
        +MoverParaArea(areaId, agora)
        +EstaAtrasada(hoje) bool
        +AdicionarItem(texto, agora) ItemChecklist
        +EditarItem(itemId, texto, agora)
        +MarcarItem(itemId, concluido, agora)
        +RemoverItem(itemId, agora)
    }

    class ItemChecklist {
        +Guid Id
        +Guid TarefaId
        +string Texto
        +bool Concluido
        +int Ordem
    }

    class RecuperacaoSenha {
        +Guid Id
        +Guid UsuarioId
        +string CodigoHash
        +DateTime CriadoEm
        +DateTime ExpiraEm
        +DateTime? UsadoEm
        +EstaValida(agora) bool
        +Encerrar(agora)
    }

    class StatusTarefa {
        <<enumeration>>
        AFazer
        EmAndamento
        Concluida
    }

    class Prioridade {
        <<enumeration>>
        Baixa
        Media
        Alta
        Urgente
    }

    class ExcecaoDominio {
        <<abstract>>
    }
    class ValidacaoException
    class RegraNegocioException
    class NaoEncontradoException

    Usuario "1" --> "1..*" Area : possui
    Usuario "1" --> "0..*" Tarefa : possui
    Usuario "1" --> "0..*" RecuperacaoSenha : solicita
    Area "1" --> "0..*" Tarefa : agrupa
    Tarefa "1" *-- "0..50" ItemChecklist : checklist
    Tarefa --> StatusTarefa
    Tarefa --> Prioridade
    ExcecaoDominio <|-- ValidacaoException
    ExcecaoDominio <|-- RegraNegocioException
    ExcecaoDominio <|-- NaoEncontradoException
```

## 5. Diagrama de classes: aplicação e infraestrutura

```mermaid
classDiagram
    direction TB

    class AutenticacaoServico {
        +CadastrarAsync(CadastroRequest)
        +EntrarAsync(LoginRequest)
        +SolicitarRecuperacaoAsync(RecuperarSenhaRequest)
        +RedefinirSenhaAsync(RedefinirSenhaRequest)
    }
    class TarefaServico {
        +ListarAsync(filtro, ordenacao, pagina, tamanho)
        +KanbanAsync(filtro)
        +CalendarioAsync(inicio, fim, filtro)
        +ObterAsync(id)
        +CriarAsync(CriarTarefaRequest)
        +AtualizarAsync(id, AtualizarTarefaRequest)
        +AlterarStatusAsync(id, AlterarStatusRequest)
        +ExcluirAsync(id)
    }
    class ChecklistServico {
        +AdicionarAsync(tarefaId, ItemRequest)
        +EditarAsync(tarefaId, itemId, ItemRequest)
        +MarcarAsync(tarefaId, itemId, MarcarItemRequest)
        +RemoverAsync(tarefaId, itemId)
    }
    class AreaServico {
        +ListarAsync()
        +ObterAsync(id)
        +CriarAsync(AreaRequest)
        +RenomearAsync(id, AreaRequest)
        +ExcluirAsync(id)
    }
    class PerfilServico {
        +ObterAsync()
        +AtualizarAsync(AtualizarPerfilRequest)
        +AlterarSenhaAsync(AlterarSenhaRequest)
        +ExcluirContaAsync(ExcluirContaRequest)
    }
    class PainelServico {
        +ObterAsync()
    }

    class IUsuarioRepositorio { <<interface>> }
    class IAreaRepositorio { <<interface>> }
    class ITarefaRepositorio { <<interface>> }
    class IRecuperacaoSenhaRepositorio { <<interface>> }
    class IUnidadeDeTrabalho { <<interface>> }
    class IHashSenha { <<interface>> }
    class IServicoToken { <<interface>> }
    class IEnviadorEmail { <<interface>> }
    class IRelogio { <<interface>> }
    class IUsuarioAtual { <<interface>> }

    class FlowDayDbContext
    class TarefaRepositorio
    class AreaRepositorio
    class UsuarioRepositorio
    class RecuperacaoSenhaRepositorio
    class HashSenhaBCrypt
    class EnviadorEmailSmtp
    class RelogioSistema
    class ServicoTokenJwt
    class UsuarioAtual

    AutenticacaoServico ..> IUsuarioRepositorio
    AutenticacaoServico ..> IHashSenha
    AutenticacaoServico ..> IServicoToken
    AutenticacaoServico ..> IEnviadorEmail
    AutenticacaoServico ..> IRecuperacaoSenhaRepositorio
    TarefaServico ..> ITarefaRepositorio
    TarefaServico ..> IAreaRepositorio
    TarefaServico ..> IUsuarioAtual
    ChecklistServico ..> TarefaServico
    AreaServico ..> IAreaRepositorio
    AreaServico ..> ITarefaRepositorio
    PerfilServico ..> IUsuarioRepositorio
    PerfilServico ..> AutenticacaoServico
    PainelServico ..> ITarefaRepositorio
    TarefaServico ..> IUnidadeDeTrabalho
    TarefaServico ..> IRelogio

    IUnidadeDeTrabalho <|.. FlowDayDbContext
    ITarefaRepositorio <|.. TarefaRepositorio
    IAreaRepositorio <|.. AreaRepositorio
    IUsuarioRepositorio <|.. UsuarioRepositorio
    IRecuperacaoSenhaRepositorio <|.. RecuperacaoSenhaRepositorio
    IHashSenha <|.. HashSenhaBCrypt
    IEnviadorEmail <|.. EnviadorEmailSmtp
    IRelogio <|.. RelogioSistema
    IServicoToken <|.. ServicoTokenJwt
    IUsuarioAtual <|.. UsuarioAtual
```

`ServicoTokenJwt` e `UsuarioAtual` ficam na camada de API, pois dependem do ASP.NET Core. Os demais adaptadores ficam na infraestrutura.

## 6. Diagrama entidade-relacionamento

Modelo físico gerado pelo Entity Framework Core no MySQL. Chaves primárias são `char(36)` (GUID gerado no domínio).

```mermaid
erDiagram
    usuarios ||--o{ areas : "possui"
    usuarios ||--o{ tarefas : "possui"
    usuarios ||--o{ recuperacoes_senha : "solicita"
    areas ||--o{ tarefas : "agrupa"
    tarefas ||--o{ itens_checklist : "contém"

    usuarios {
        char(36) Id PK
        varchar(100) Nome
        varchar(254) Email
        varchar(254) EmailNormalizado UK
        varchar(100) SenhaHash
        int FalhasLogin
        datetime BloqueadoAte "nulo"
        datetime SenhaAlteradaEm
        datetime CriadoEm
    }

    areas {
        char(36) Id PK
        char(36) UsuarioId FK
        varchar(50) Nome
        varchar(50) NomeNormalizado "UK com UsuarioId"
        tinyint EhGeral
    }

    tarefas {
        char(36) Id PK
        char(36) UsuarioId FK
        char(36) AreaId FK
        varchar(120) Titulo
        varchar(1000) Descricao "nulo"
        int Status "1 A fazer, 2 Em andamento, 3 Concluída"
        int Prioridade "1 Baixa a 4 Urgente"
        date Prazo "nulo"
        datetime ConcluidaEm "nulo"
        datetime CriadaEm
        datetime AtualizadaEm
    }

    itens_checklist {
        char(36) Id PK
        char(36) TarefaId FK
        varchar(200) Texto
        tinyint Concluido
        int Ordem
    }

    recuperacoes_senha {
        char(36) Id PK
        char(36) UsuarioId FK
        varchar(64) CodigoHash UK "SHA-256"
        datetime CriadoEm
        datetime ExpiraEm
        datetime UsadoEm "nulo"
    }
```

Regras de integridade:

| Relacionamento | Exclusão |
|---|---|
| usuarios → areas, tarefas, recuperacoes_senha | Cascata (excluir a conta remove tudo, RN21) |
| areas → tarefas | Restrita: as tarefas são movidas para a Geral antes (RN18) |
| tarefas → itens_checklist | Cascata (excluir a tarefa remove os itens, RN21) |

Índices: `usuarios.EmailNormalizado` (único, RN02), `areas (UsuarioId, NomeNormalizado)` (único, RN16), `tarefas (UsuarioId, Status)` e `tarefas (UsuarioId, Prazo)` (Kanban, calendário e painel), `recuperacoes_senha.CodigoHash` (único).

## 7. Diagrama de estados da tarefa

```mermaid
stateDiagram-v2
    [*] --> AFazer : criar (RN08)
    AFazer --> EmAndamento
    EmAndamento --> AFazer
    AFazer --> Concluida : todos os itens concluídos (RN10)
    EmAndamento --> Concluida : todos os itens concluídos (RN10)
    Concluida --> AFazer : reabrir (apaga ConcluidaEm, RN11)
    Concluida --> EmAndamento : reabrir (apaga ConcluidaEm, RN11)
    Concluida --> [*] : excluir
    AFazer --> [*] : excluir
    EmAndamento --> [*] : excluir

    note right of Concluida
        Registra ConcluidaEm (RN11).
        Checklist bloqueado (RN12).
    end note
```

## 8. Diagrama de sequência: login

```mermaid
sequenceDiagram
    autonumber
    actor V as Visitante
    participant FE as Front-end
    participant API as AutenticacaoController
    participant S as AutenticacaoServico
    participant U as Usuario (domínio)
    participant R as IUsuarioRepositorio
    participant H as IHashSenha (BCrypt)
    participant T as IServicoToken (JWT)

    V->>FE: informa e-mail e senha
    FE->>API: POST /api/auth/login
    Note over API: Rate limiting: 10 req/min por IP (RNF08)
    API->>S: EntrarAsync(req)
    S->>R: ObterPorEmailAsync(emailNormalizado)
    alt e-mail inexistente
        S->>H: Gerar(senha) — mesmo custo, não revela o e-mail
        S-->>API: NaoAutorizadoException
        API-->>FE: 401 "E-mail ou senha incorretos."
    else conta bloqueada
        S->>U: EstaBloqueado(agora)
        S-->>API: ContaBloqueadaException
        API-->>FE: 423 Login bloqueado (RN04)
    else senha incorreta
        S->>H: Verificar(senha, hash) = false
        S->>U: RegistrarFalhaLogin(agora) — 5ª falha bloqueia por 15 min
        S-->>API: NaoAutorizadoException
        API-->>FE: 401 mensagem genérica (RN04)
    else credenciais corretas
        S->>H: Verificar(senha, hash) = true
        S->>U: RegistrarLoginComSucesso()
        S->>T: Gerar(usuario) — expira em 60 min, claim "sv"
        S-->>API: AutenticacaoResponse
        API-->>FE: 200 { token, expiraEm, usuario }
        FE->>FE: guarda o token e abre o painel
    end
```

## 9. Diagrama de sequência: concluir tarefa

```mermaid
sequenceDiagram
    autonumber
    actor U as Usuário
    participant FE as Front-end (Kanban)
    participant JWT as Middleware JWT
    participant C as TarefasController
    participant S as TarefaServico
    participant R as ITarefaRepositorio
    participant T as Tarefa (domínio)
    participant DB as MySQL

    U->>FE: arrasta o cartão para "Concluída"
    FE->>C: PATCH /api/tarefas/{id}/status { "status": "Concluida" }
    JWT->>JWT: valida assinatura, expiração e carimbo "sv" (RN05)
    C->>S: AlterarStatusAsync(id, req)
    S->>R: ObterAsync(id, usuarioId)
    R->>DB: SELECT ... WHERE Id = @id AND UsuarioId = @usuario (RN01)
    alt tarefa de outro usuário ou inexistente
        S-->>C: NaoEncontradoException
        C-->>FE: 404 Não encontrado
    else tarefa encontrada
        S->>T: AlterarStatus(Concluida, agora)
        alt há item pendente
            T-->>S: RegraNegocioException (RN10)
            S-->>C: exceção
            C-->>FE: 422 "A tarefa só pode ser concluída quando todos os itens..."
            FE->>U: mostra o motivo e mantém o cartão na coluna
        else todos os itens concluídos
            T->>T: Status = Concluida, ConcluidaEm = agora (RN11)
            S->>DB: SaveChanges
            S-->>C: TarefaResponse
            C-->>FE: 200 tarefa atualizada
        end
    end
```

## 10. Diagrama de sequência: recuperação de senha

```mermaid
sequenceDiagram
    autonumber
    actor V as Visitante
    participant FE as Front-end
    participant S as AutenticacaoServico
    participant RR as IRecuperacaoSenhaRepositorio
    participant M as IEnviadorEmail (SMTP)
    participant U as Usuario (domínio)

    V->>FE: "Esqueci minha senha" + e-mail
    FE->>S: POST /api/auth/recuperar-senha
    S->>S: busca o usuário pelo e-mail
    opt conta existe
        S->>RR: ListarAtivasAsync → Encerrar() (novo pedido invalida o anterior)
        S->>S: código aleatório de 32 bytes
        S->>RR: Adicionar(hash SHA-256 do código, validade de 30 min)
        S->>M: e-mail com link redefinir-senha.html?token=código
    end
    S-->>FE: 202 mesma mensagem, exista ou não o e-mail (RN06)

    V->>FE: abre o link e define a nova senha
    FE->>S: POST /api/auth/redefinir-senha { token, novaSenha }
    S->>RR: ObterPorHashAsync(SHA-256(token))
    alt link inválido, expirado ou já usado
        S-->>FE: 422 "Link inválido, expirado ou já utilizado."
    else link válido
        S->>U: AlterarSenha(novoHash) — zera falhas e muda o carimbo "sv"
        S->>RR: Encerrar() — uso único
        S-->>FE: 200 "Senha redefinida com sucesso."
        Note over FE: tokens emitidos antes deixam de valer (RN05)
    end
```

## 11. Diagrama de sequência: excluir área

```mermaid
sequenceDiagram
    autonumber
    actor U as Usuário
    participant FE as Front-end
    participant C as AreasController
    participant S as AreaServico
    participant A as Area (domínio)
    participant TR as ITarefaRepositorio
    participant DB as MySQL

    U->>FE: Excluir área "Estudos"
    FE->>U: confirmação (RN21)
    U->>FE: confirma
    FE->>C: DELETE /api/areas/{id}
    C->>S: ExcluirAsync(id)
    S->>A: GarantirQuePodeSerExcluida()
    alt área Geral
        A-->>S: RegraNegocioException (RN17)
        S-->>C: exceção
        C-->>FE: 422 "A área Geral não pode ser excluída."
    else outra área
        S->>TR: ListarDaAreaAsync(id)
        loop cada tarefa da área
            S->>S: tarefa.MoverParaArea(geral.Id) (RN18)
        end
        S->>DB: SaveChanges — UPDATEs e DELETE na mesma transação (RNF21)
        C-->>FE: 204 No Content
    end
```
