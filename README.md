# RFID Reader para Raspberry Pi

Leitor RFID otimizado para execucao no Raspberry Pi.
Compilado como .NET 8 Self-Contained - nao precisa instalar runtime.

## Estrutura do Projeto

```
RfidReaderRaspberry/
├── src/                          # Codigo fonte
│   ├── Program.cs               # Ponto de entrada
│   ├── RfidReader.cs            # Wrapper do leitor
│   └── TagCounter.cs            # Contador thread-safe
├── scripts/
│   ├── build.bat                # Build e publish
│   ├── deploy.bat               # Deploy via SCP
│   └── run-raspberry.sh         # Execucao no Raspberry
├── bin/Release/net8.0/linux-arm64/publish/  # Saida do build
└── RfidReaderRaspberry.csproj   # Projeto .NET 8
```

## Build

### Requisitos (Windows)
- .NET 8 SDK

### Compilar

```batch
cd scripts
build.bat
```

Arquivos gerados em: `bin\Release\net8.0\linux-arm64\publish\`

## Deploy

### Opcao 1: Via Script (SCP)

```batch
scripts\deploy.bat
```

### Opcao 2: Manual

Copie TODO o conteudo da pasta `publish` para o Raspberry:
```
bin\Release\net8.0\linux-arm64\publish\*
```

## Execucao no Raspberry Pi

```bash
cd /home/cronoserv/teste_leitor
chmod +x RfidReaderRaspberry
./RfidReaderRaspberry [IP_LEITOR] [DURACAO]
```

Exemplos:
```bash
./RfidReaderRaspberry                    # IP padrao, 30 segundos
./RfidReaderRaspberry 192.168.0.242      # IP especifico, 30 segundos
./RfidReaderRaspberry 192.168.0.242 60   # IP especifico, 60 segundos
```

## Modos de Operacao

### Single Target (Largada)
- Session 0
- Detecta tag uma vez
- Ideal para registro de largada

### Dual Target (Chegada)
- Session 1
- Detecta tag continuamente
- Ideal para registro de chegada/voltas

## Configuracoes RF

| Parametro | Single Target | Dual Target |
|-----------|--------------|-------------|
| SearchMode | SingleTarget | DualTarget |
| Session | 0 | 1 |
| Population | 16 | 32 |
| ReaderMode | MaxThroughput | MaxThroughput |

## Notas

- **Nao precisa de Mono** - O executavel e self-contained
- **Nao precisa instalar .NET no Raspberry** - Tudo incluido
- Tamanho do pacote: ~79 MB (inclui runtime .NET 8)
- Compativel com Raspberry Pi 3/4/5 (ARM64)
