FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /App

RUN curl -sL https://deb.nodesource.com/setup_18.x | bash
RUN apt-get update && apt-get install -y nodejs

# Copy everything
COPY . ./
# Restore as distinct layers
RUN ls
# Build and publish a release
RUN dotnet publish -c Release -o out --nologo ./BoardGameTracker.Host/BoardGameTracker.Host.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /tracker
COPY --from=build-env /App/out .
EXPOSE 7178/tcp

ENV ENVIRONMENT=production
ENV DOTNET_EnableDiagnostics=0
ENV ASPNETCORE_URLS=http://*
ENTRYPOINT ["dotnet", "BoardGameTracker.Host.dll"]