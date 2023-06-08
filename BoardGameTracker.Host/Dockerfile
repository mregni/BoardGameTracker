FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BoardGameTracker.Host/BoardGameTracker.Host.csproj", "BoardGameTracker.Host/"]
RUN dotnet restore "BoardGameTracker.Host/BoardGameTracker.Host.csproj"
COPY . .
WORKDIR "/src/BoardGameTracker.Host"
RUN dotnet build "BoardGameTracker.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BoardGameTracker.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BoardGameTracker.Host.dll"]
