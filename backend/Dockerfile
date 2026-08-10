FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/ ./
RUN dotnet restore src/DesfudenciFy.Api/DesfudenciFy.Api.csproj
RUN dotnet publish src/DesfudenciFy.Api/DesfudenciFy.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN mkdir -p /app/data/uploads
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DesfudenciFy.Api.dll"]
