# BoardGameTracker

<p align="center">
  <img src=".github/images/games.png" width="800" alt="screen preview">
  <br/>
  <br/>
  BoardGameTracker is a selfhosted board game statistics tracker that can be hosted in a docker container.
</p>

## Docker command
```
docker run -e DB_HOST=<DB_HOST> -e DB_USER=<DB_USER> -e DB_PASSWORD=<DB_PASSWORD> -e DB_NAME=<DB_NAME>
 -e DB_PORT=<DB_PORT> -p <HOST_PORT>:5444 uping/boardgametracker
```

## Docker compose
```
version: "3.8"
services:
  boardgametracker:
    image: uping/boardgametracker:latest
    restart: unless-stopped
    volumes:
      - <IMAGE_PATH>:/app/images
      - <DATA_PATH>:/app/data
    ports:
      - 5444:5444
    environment:
      - DB_HOST=db
      - DB_USER=dev
      - DB_PASSWORD=CHANGEME
      - DB_NAME=boardgametracker
      - DB_PORT=5432

  db:
    image: postgres:16
    restart: unless-stopped
    volumes:
      - <DB_PATH>:/var/lib/postgresql/data
    environment:
      - POSTGRES_DB=boardgametracker
      - POSTGRES_USER=dev
      - POSTGRES_PASSWORD=CHANGEME
    ports:
      - 5432:5432
```

You can also download the docker compose example [here](docker-compose.example.yml)

## Disclaimer

- ⚠️ The project is still in alpha.

# Tooling
- This project is tested with BrowserStack.
- Translations are managed with Crowdin
