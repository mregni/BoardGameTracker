FROM mcr.microsoft.com/dotnet/sdk:8.0 AS with-node
RUN apt-get update
RUN apt-get install curl
RUN curl -sL https://deb.nodesource.com/setup_20.x | bash
RUN apt-get -y install nodejs

FROM with-node AS publish
WORKDIR /src
COPY . .
RUN dotnet restore "./BoardGameTracker.Host/BoardGameTracker.Host.csproj"
RUN dotnet publish "./BoardGameTracker.Host/BoardGameTracker.Host.csproj" -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /publish .
ENV ASPNETCORE_ENVIRONMENT=production
ENV DOTNET_EnableDiagnostics=0
ENV ASPNETCORE_URLS=http://*:5444
EXPOSE 5444
ENTRYPOINT ["dotnet", "BoardGameTracker.Host.dll"]
