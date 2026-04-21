FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY src/ ./src/
RUN dotnet restore src/Bootstrap/Api/Api.csproj

RUN dotnet publish src/Bootstrap/Api/Api.csproj -c Release -o /api --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /api
COPY --from=build /api ./
ENTRYPOINT ["dotnet", "Api.dll"]
