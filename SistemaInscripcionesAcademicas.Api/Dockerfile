FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY SistemaInscripcionesAcademicas.Api/SistemaInscripcionesAcademicas.Api.csproj SistemaInscripcionesAcademicas.Api/
RUN dotnet restore SistemaInscripcionesAcademicas.Api/SistemaInscripcionesAcademicas.Api.csproj
COPY SistemaInscripcionesAcademicas.Api/ SistemaInscripcionesAcademicas.Api/
RUN dotnet publish SistemaInscripcionesAcademicas.Api/SistemaInscripcionesAcademicas.Api.csproj --configuration Release --no-restore --output /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out/ ./
RUN mkdir -p /app/keys /app/logs && chown "$APP_UID:$APP_UID" /app/keys /app/logs
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SistemaInscripcionesAcademicas.Api.dll"]
