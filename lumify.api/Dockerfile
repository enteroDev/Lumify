# ---- Build-Stufe: kompiliert die .NET-API ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Erst nur die csproj kopieren + restore (nutzt Docker-Cache, schnellere Re-Builds)
COPY *.csproj ./
RUN dotnet restore

# Dann den Rest des API-Codes kopieren und veröffentlichen
COPY . ./
RUN dotnet publish -c Release -o /app /p:UseAppHost=false

# ---- Lauf-Stufe: schlankes Runtime-Image, nur das fertige Programm ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS run
WORKDIR /app
COPY --from=build /app ./

# Im Container immer Production + Port 8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "lumify.api.dll"]
