# Estágio 1: compila e publica
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/MagnesiumCoffee.Api/MagnesiumCoffee.Api.csproj src/MagnesiumCoffee.Api/
RUN dotnet restore src/MagnesiumCoffee.Api/MagnesiumCoffee.Api.csproj
COPY src/ src/
RUN dotnet publish src/MagnesiumCoffee.Api/MagnesiumCoffee.Api.csproj -c Release -o /app/publish --no-restore

# Estágio 2: só o runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MagnesiumCoffee.Api.dll"]
