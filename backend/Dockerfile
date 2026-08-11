FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/ ./
RUN dotnet restore src/DesfudenciFy.Api/DesfudenciFy.Api.csproj
RUN dotnet publish src/DesfudenciFy.Api/DesfudenciFy.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /app/data/uploads
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker
COPY --from=build /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=5s --start-period=40s --retries=5 \
  CMD curl -fsS http://127.0.0.1:8080/health || exit 1
ENTRYPOINT ["dotnet", "DesfudenciFy.Api.dll"]
