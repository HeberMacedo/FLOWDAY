# Etapa 1: restaura e compila a solução
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FlowDay.sln Directory.Build.props ./
COPY src/FlowDay.Domain/FlowDay.Domain.csproj src/FlowDay.Domain/
COPY src/FlowDay.Application/FlowDay.Application.csproj src/FlowDay.Application/
COPY src/FlowDay.Infrastructure/FlowDay.Infrastructure.csproj src/FlowDay.Infrastructure/
COPY src/FlowDay.Api/FlowDay.Api.csproj src/FlowDay.Api/
COPY tests/FlowDay.Tests/FlowDay.Tests.csproj tests/FlowDay.Tests/
RUN dotnet restore FlowDay.sln

COPY . .
RUN dotnet build FlowDay.sln -c Release --no-restore

# Etapa opcional: executa os testes automatizados
#   docker build --target testes -t flowday-testes .
FROM build AS testes
RUN dotnet test FlowDay.sln -c Release --no-build --logger "console;verbosity=normal"

# Etapa 2: publica a API
FROM build AS publish
RUN dotnet publish src/FlowDay.Api/FlowDay.Api.csproj -c Release -o /app --no-build

# Etapa 3: imagem final, só com o runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "FlowDay.Api.dll"]
