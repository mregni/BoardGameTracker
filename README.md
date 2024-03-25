# BoardGameTracker

<p align="center">
  <img src=".github/images/games.png" width="600" alt="screen preview">
  <br/>
  <br/>
  BoardGameTracker is a selfhosted board game statistics tracker that can be hosted with docker.
</p>

## Docker command
```
docker run -e DB_HOST=<DB_HOST> -e DB_USER=<DB_USER> -e DB_PASSWORD=<DB_PASSWORD> -e DB_NAME=<DB_NAME>
 -e DB_PORT=<DB_PORT> -p <HOST_PORT>:5444 uping/boardgametracker
```

## Docker compose
```
docker run -e DB_HOST=<DB_HOST> -e DB_USER=<DB_USER> -e DB_PASSWORD=<DB_PASSWORD> -e DB_NAME=<DB_NAME>
 -e DB_PORT=<DB_PORT> -p <HOST_PORT>:5444 uping/boardgametracker
```

## Disclaimer

- ⚠️ The project is still in alpha.