FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NormalMi.API/NormalMi.API.csproj NormalMi.API/
RUN dotnet restore NormalMi.API/NormalMi.API.csproj

COPY NormalMi.API/ NormalMi.API/
WORKDIR /src/NormalMi.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "NormalMi.API.dll"]
