@echo off
REM =====================================================
REM  Script de Deploy para Raspberry Pi via SCP
REM  .NET 8 Self-Contained
REM =====================================================

echo.
echo =====================================================
echo  Deploy para Raspberry Pi
echo =====================================================
echo.

set PROJECT_DIR=%~dp0..
set OUTPUT_DIR=%PROJECT_DIR%\bin\Release\net8.0\linux-arm64\publish

REM Configuracoes do Raspberry (altere conforme necessario)
set RPI_USER=cronoserv
set RPI_HOST=pi4cronoserv
set RPI_PATH=/home/cronoserv/teste_leitor

REM Verifica se os arquivos existem
if not exist "%OUTPUT_DIR%\RfidReaderRaspberry" (
    echo ERRO: Executavel nao encontrado!
    echo Execute o build primeiro: scripts\build.bat
    pause
    exit /b 1
)

echo Configuracoes:
echo   Usuario: %RPI_USER%
echo   Host: %RPI_HOST%
echo   Destino: %RPI_PATH%
echo.

set /p CONFIRM="Deseja continuar com o deploy? (S/N): "
if /i not "%CONFIRM%"=="S" (
    echo Deploy cancelado.
    pause
    exit /b 0
)

echo.
echo Criando diretorio no Raspberry...
ssh %RPI_USER%@%RPI_HOST% "mkdir -p %RPI_PATH%"

echo.
echo Copiando arquivos (isso pode demorar)...
scp -r "%OUTPUT_DIR%\*" %RPI_USER%@%RPI_HOST%:%RPI_PATH%/

echo.
echo Ajustando permissoes...
ssh %RPI_USER%@%RPI_HOST% "chmod +x %RPI_PATH%/RfidReaderRaspberry"

echo.
echo =====================================================
echo  Deploy concluido!
echo =====================================================
echo.
echo Para executar no Raspberry:
echo   ssh %RPI_USER%@%RPI_HOST%
echo   cd %RPI_PATH%
echo   ./RfidReaderRaspberry [IP_LEITOR] [DURACAO]
echo.

pause
