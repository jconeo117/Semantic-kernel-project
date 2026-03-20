# Docker container for ReceptionistAgent.Api

# Etapa 1: Construcción (Build)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivos de solución y proyectos
COPY ["src/ReceptionistAgent.Api/ReceptionistAgent.Api.csproj", "src/ReceptionistAgent.Api/"]
COPY ["src/ReceptionistAgent.Core/ReceptionistAgent.Core.csproj", "src/ReceptionistAgent.Core/"]
COPY ["src/ReceptionistAgent.Connectors/ReceptionistAgent.Connectors.csproj", "src/ReceptionistAgent.Connectors/"]
COPY ["src/ReceptionistAgent.AI/ReceptionistAgent.AI.csproj", "src/ReceptionistAgent.AI/"]

# Restaurar dependencias
RUN dotnet restore "src/ReceptionistAgent.Api/ReceptionistAgent.Api.csproj"

# Copiar todo el código fuente
COPY . .

# Compilar y publicar la app
WORKDIR "/src/src/ReceptionistAgent.Api"
RUN dotnet publish "ReceptionistAgent.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Ejecución (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Cloud Run requiere que la aplicación escuche en el puerto 8080 (por defecto a través del la variable ambiental PORT)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ReceptionistAgent.Api.dll"]
