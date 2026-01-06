# Etapa de construcción
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FreelanceGuard.Api.csproj", "./"]
RUN dotnet restore "FreelanceGuard.Api.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "FreelanceGuard.Api.csproj" -c Release -o /app/build

# Etapa de publicación
FROM build AS publish
RUN dotnet publish "FreelanceGuard.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa final (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FreelanceGuard.Api.dll"]