FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/GameServer/GameServer.csproj src/GameServer/
RUN dotnet restore src/GameServer/GameServer.csproj
COPY src/GameServer/ src/GameServer/
RUN dotnet publish src/GameServer/GameServer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 9000 11111 30000
ENTRYPOINT ["dotnet", "GameServer.dll"]
