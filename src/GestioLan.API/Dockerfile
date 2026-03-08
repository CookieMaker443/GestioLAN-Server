# FASE 1: Build e Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia il file di progetto per fare il restore delle librerie
COPY ["GestioLan.API.csproj", "./"]
RUN dotnet restore

# Copia tutto il codice e pubblica la release
COPY . .
RUN dotnet publish "GestioLan.API.csproj" -c Release -o /app/publish

# FASE 2: Runtime finale (Immagine leggera)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copia solo la release dalla fase precedente ed espone la porta interna del container
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "GestioLan.API.dll"]