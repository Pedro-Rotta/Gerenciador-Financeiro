# Imagem do Gerenciador Familiar: telas React + API .NET 8 num contêiner só.
# Usada pela Render (servidor) e pelo ensaio local (docker-compose.local.yml).

# 1) Telas: compila o React (inclui a checagem de tipos do tsc)
FROM node:24-slim AS front
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# 2) API: restaura e publica em Release
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api
WORKDIR /src
COPY GerenciadorContaFamiliar/ GerenciadorContaFamiliar/
RUN dotnet publish GerenciadorContaFamiliar/GerenciadorContaFamiliar.csproj -c Release -o /app/publish /p:UseAppHost=false

# 3) Final: só o runtime (Debian: fuso horário e ICU para pt-BR)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080
COPY --from=api /app/publish ./
COPY --from=front /src/frontend/dist ./wwwroot
EXPOSE 8080
# Usuário sem privilégios que a imagem do .NET já traz
USER $APP_UID
ENTRYPOINT ["dotnet", "GerenciadorContaFamiliar.dll"]
