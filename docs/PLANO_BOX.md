# Plano de Desenvolvimento - BOX (Raspberry Pi)

## Visão Geral

O BOX é uma unidade autônoma controlada remotamente via API HTTP.
Pode ser orquestrado pelo **App Android** ou **Programa Gerenciador**.

---

## Funcionalidades do BOX

### 1. Configuração (via API)
- `evento_id` - ID do evento atual
- `checkpoint` - Tipo do ponto (P01, C01, V01, etc.)
- `leitores[]` - Lista de leitores (1 ou 2)
  - IP do leitor
  - Modo (SingleTarget/DualTarget)
  - Antenas habilitadas

### 2. Modos de Operação

| Modo | Descrição |
|------|-----------|
| **Teste** | Mostra contagem de leituras por tag (sem persistir) |
| **Leitura** | Consolida leituras → eventos de passagem → SQLite |
| **Parado** | Desconectado dos leitores |

### 3. Persistência Local
- SQLite com WAL mode
- Tabela `eventos_passagem`
- Flag `enviado` para controle de sync

---

## API HTTP do BOX

```
GET  /status          → Estado atual (modo, leitores, contadores)
POST /config          → Configurar evento, checkpoint, leitores
POST /start           → Iniciar leitura (modo normal)
POST /stop            → Parar leitura
POST /test/start      → Iniciar modo teste
POST /test/stop       → Parar modo teste
GET  /test/results    → Resultados do teste (tags x contagem)
GET  /events          → Listar eventos para sync
POST /events/ack      → Marcar eventos como enviados
GET  /health          → Health check simples
```

---

## Estrutura do Projeto BOX

```
RfidReaderRaspberry/
├── src/
│   ├── Program.cs              # Ponto de entrada
│   ├── Services/
│   │   ├── RfidService.cs      # Gerencia leitores RFID
│   │   ├── EventService.cs     # Consolidação e persistência
│   │   └── TestService.cs      # Modo teste de antenas
│   ├── Api/
│   │   └── BoxController.cs    # Endpoints da API
│   ├── Data/
│   │   ├── BoxDbContext.cs     # SQLite context
│   │   └── EventoPassagem.cs   # Modelo
│   └── Models/
│       ├── BoxConfig.cs        # Configuração do BOX
│       └── LeitorConfig.cs     # Configuração de cada leitor
├── appsettings.json            # Config padrão para dev/teste
└── RfidReaderRaspberry.csproj
```

---

## Banco de Dados (SQLite)

### Tabela: eventos_passagem
```sql
CREATE TABLE eventos_passagem (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    box_id TEXT NOT NULL,
    evento_id TEXT NOT NULL,
    checkpoint TEXT NOT NULL,
    epc TEXT NOT NULL,
    timestamp TEXT NOT NULL,
    lap INTEGER DEFAULT 1,
    leitor_ip TEXT,
    antena INTEGER,
    rssi REAL,
    enviado INTEGER DEFAULT 0,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);
```

### Tabela: config
```sql
CREATE TABLE config (
    key TEXT PRIMARY KEY,
    value TEXT
);
```

---

## Fluxo de Desenvolvimento

### Etapa 1: Estrutura base + API
- [ ] Criar projeto ASP.NET Core minimal
- [ ] Implementar endpoints básicos (/status, /health)
- [ ] Arquivo appsettings.json para config de desenvolvimento

### Etapa 2: Modo Teste
- [ ] Endpoint /test/start - inicia leitura de teste
- [ ] Endpoint /test/stop - para teste
- [ ] Endpoint /test/results - retorna tags x contagem
- [ ] Não persiste, apenas conta

### Etapa 3: Persistência SQLite
- [ ] Configurar Entity Framework Core + SQLite
- [ ] Criar tabelas
- [ ] Migrations

### Etapa 4: Modo Leitura Normal
- [ ] Consolidação de leituras RF → evento de passagem
- [ ] Janela de tempo para evitar duplicatas
- [ ] Persistir no SQLite

### Etapa 5: Configuração via API
- [ ] Endpoint /config para receber configurações
- [ ] Suporte a 1 ou 2 leitores
- [ ] Validação de configuração

### Etapa 6: Sync de eventos
- [ ] Endpoint /events para listar eventos não enviados
- [ ] Endpoint /events/ack para marcar como enviados

### Etapa 7: Serviço systemd
- [ ] Criar arquivo de serviço
- [ ] Iniciar no boot automaticamente

---

## Para Testar Durante Desenvolvimento

Sem App Android, usamos:
1. **appsettings.json** - configuração padrão
2. **curl/Postman** - chamar API manualmente
3. **Swagger** - interface web para testar endpoints

---

## Arquivos Críticos

| Arquivo | Função |
|---------|--------|
| `appsettings.json` | Config padrão (IP leitor, evento teste) |
| `BoxController.cs` | Todos os endpoints da API |
| `RfidService.cs` | Conexão e leitura dos readers |
| `EventService.cs` | Consolidação e persistência |

---

## Próxima Ação

Começar pela **Etapa 1**: Criar estrutura ASP.NET Core minimal com API básica.

Isso permite testar a comunicação com o BOX via HTTP enquanto desenvolvemos as outras funcionalidades.
