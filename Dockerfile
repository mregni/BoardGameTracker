# docker run -e DB_HOST=192.168.72.66 -e DB_USER=dev -e DB_PASSWORD=dev -e DB_NAME=boardgametracker-dev -p 6544:5444 -v C:\Users\mikha\Documents\Repositories\BoardGameTracker\BoardGameTracker.Host\data:/app/data -v C:\Users\mikha\Documents\Repositories\BoardGameTracker\BoardGameTracker.Host\images:/app/images
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY ./publish .
ENV ASPNETCORE_ENVIRONMENT=production
ENV DOTNET_EnableDiagnostics=0
ENV ASPNETCORE_URLS=http://*:5444
EXPOSE 5444
ENTRYPOINT ["dotnet", "BoardGameTracker.Host.dll"]
