@echo off
REM =====================================================
REM  Build Script para RFID Reader Raspberry
REM  .NET 8 Self-Contained para Linux ARM64
REM =====================================================

echo.
echo =====================================================
echo  RFID Reader Raspberry - Build
echo =====================================================
echo.

set PROJECT_DIR=%~dp0..
set OUTPUT_DIR=%PROJECT_DIR%\bin\Release\net8.0\linux-arm64\publish

REM Verifica se o dotnet esta instalado
where dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERRO: dotnet CLI nao encontrado!
    echo Instale o .NET SDK: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Compilando e publicando para Raspberry Pi (linux-arm64)...
echo.

cd /d "%PROJECT_DIR%"
dotnet publish -c Release -r linux-arm64 --self-contained true

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERRO: Build falhou!
    pause
    exit /b 1
)

echo.
echo =====================================================
echo  Build concluido com sucesso!
echo =====================================================
echo.
echo Arquivos de saida em:
echo   %OUTPUT_DIR%
echo.
echo Copie TODO o conteudo da pasta publish para o Raspberry.
echo.
echo No Raspberry, execute:
echo   chmod +x RfidReaderRaspberry
echo   ./RfidReaderRaspberry
echo.

pause
