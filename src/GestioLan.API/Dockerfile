# FASE 1: Build e Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


# Copia prima i file .csproj per sfruttare la cache dei layer
COPY ["GestioLan.API/GestioLan.API.csproj", "GestioLan.API/"]
COPY ["Plugins/Shared/Plugins.Shared.csproj", "Plugins/Shared/"]
RUN dotnet restore "GestioLan.API/GestioLan.API.csproj"

# Copia tutto il codice e pubblica la release
COPY . .
RUN dotnet publish "GestioLan.API/GestioLan.API.csproj" -c Release -o /app/publish

# Pubblica Plugins.Shared separatamente in una cartella dedicata.
# --no-self-contained: non include il runtime .NET nell'output, perche
# il plugin viene caricato dentro il processo dell'API che ha gia .NET 8.
# Produce: Plugins.Shared.dll e Plugins.Shared.deps.json
RUN dotnet publish "Plugins/Shared/Plugins.Shared.csproj" \
    -c Release \
    --no-self-contained \
    -o /app/plugins/Plugins.Shared

# FASE 2: Runtime finale (Immagine leggera)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copia solo la release dalla fase precedente ed espone la porta interna del container
COPY --from=build /app/publish .

# Crea le cartelle necessarie e un utente non-root
RUN adduser --disabled-password --gecos "" appuser && \
    mkdir -p /app/data/uploads/users \
             /app/data/uploads/items \
             /app/plugins && \
    chown -R appuser /app

USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "GestioLan.API.dll"]