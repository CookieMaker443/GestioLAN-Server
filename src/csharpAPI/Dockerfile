# FASE 1: Build e Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia il file di progetto per fare il restore delle librerie
COPY ["src/csharpAPI/csharpAPI.csproj", "src/csharpAPI/"]
RUN dotnet restore "src/csharpAPI/csharpAPI.csproj"

# Copia tutto il codice e pubblica la release
COPY . .
WORKDIR "/src/src/csharpAPI"
RUN dotnet publish "csharpAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# FASE 2: Runtime finale (Immagine leggera)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# Copia solo la release dalla fase precedente
COPY --from=build /app/publish .

# Espone la porta interna del container
EXPOSE 8080

ENTRYPOINT ["dotnet", "csharpAPI.dll"]