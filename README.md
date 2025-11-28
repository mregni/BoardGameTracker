# BoardGameTracker

<p align="center">
  <img src=".github/images/games.png" width="800" alt="BoardGameTracker screenshot">
</p>

<p align="center">
  <strong>A self-hosted board game statistics tracker for analyzing your collection and gaming sessions</strong>
</p>

<p align="center">
  <a href="https://sonarcloud.io/summary/new_code?id=mregni_BoardGameTracker">
    <img src="https://sonarcloud.io/api/project_badges/measure?project=mregni_BoardGameTracker&metric=coverage" alt="Coverage">
  </a>
  <a href="https://sonarcloud.io/summary/new_code?id=mregni_BoardGameTracker">
    <img src="https://sonarcloud.io/api/project_badges/measure?project=mregni_BoardGameTracker&metric=alert_status" alt="Quality Gate Status">
  </a>
  <a href="https://hub.docker.com/r/uping/boardgametracker">
    <img src="https://img.shields.io/docker/pulls/uping/boardgametracker" alt="Docker Pulls">
  </a>
  <a href="https://github.com/mregni/BoardGameTracker/blob/master/LICENSE">
    <img src="https://img.shields.io/github/license/mregni/BoardGameTracker" alt="License">
  </a>
</p>

---

## Overview

BoardGameTracker is a self-hosted application designed for board game enthusiasts who want to:
- Track spending on their collection
- Analyze game statistics and identify favorites
- Monitor player performance and win rates
- Visualize gaming trends over time

**Integration with BoardGameGeek (BGG)** allows seamless import of your game collection.

> ⚠️ **Note**: This project is under active development. Breaking changes may occur between releases.

---



## Features & Roadmap

| Feature                                 | Status |
|-----------------------------------------|:------:|
| Import games from BoardGameGeek         | ✅    |
| User & game list management             | ✅    |
| Game details and statistics             | ✅    |
| Dashboard statistics                    | 🚧    |
| Comprehensive logging                   | 🚧    |
| Settings configuration                  | 🚧    |
| Multi-language support                  | 🚧    |
| Manual game entry                       | 📋    |
| Player vs player statistics             | 📋    |
| Advanced game detail graphs             | 📋    |
| User profile statistics                 | 📋    |
| Game session details view               | 📋    |

**Legend:** ✅ Complete | 🚧 In Progress | 📋 Planned

## Installation

### Prerequisites
- Docker installed on your system
- PostgreSQL database (can be run via Docker Compose)

### Quick Start with Docker Compose (Recommended)

1. Download the [docker-compose.yml](docker-compose.yml) file or create one with the following content:

```yaml
version: "3.8"
services:
  boardgametracker:
    image: uping/boardgametracker:latest
    container_name: boardgametracker
    restart: unless-stopped
    volumes:
      - <IMAGE_PATH>:/app/images
      - <DATA_PATH>:/app/data
    ports:
      - 5444:5444
    environment:
      - DB_HOST=db
      - DB_USER=dev
      - DB_PASSWORD=CHANGEME  # Change this!
      - DB_NAME=boardgametracker
      - DB_PORT=5432
      - STATISTICS=0
      - TZ=Europe/London
    depends_on:
      - db

  db:
    image: postgres:16
    container_name: boardgametracker-db
    restart: unless-stopped
    volumes:
      - <DB_PATH>:/var/lib/postgresql/data
    environment:
      - POSTGRES_DB=boardgametracker
      - POSTGRES_USER=dev
      - POSTGRES_PASSWORD=CHANGEME  # Change this!
    ports:
      - 5432:5432
```

2. Update the placeholder values:
   - Replace `<IMAGE_PATH>`, `<DATA_PATH>`, and `<DB_PATH>` with your actual paths
   - Change `CHANGEME` passwords to secure values
   - Adjust timezone (`TZ`) to your location

3. Start the containers:
```bash
docker-compose up -d
```

4. Access the application at `http://localhost:5444`

### Docker Run Command

If you prefer using Docker CLI or have an existing PostgreSQL instance:

```bash
docker run \
  --name boardgametracker \
  --restart unless-stopped \
  -e DB_HOST=<DB_HOST> \
  -e DB_USER=<DB_USER> \
  -e DB_PASSWORD=<DB_PASSWORD> \
  -e DB_NAME=<DB_NAME> \
  -e DB_PORT=<DB_PORT> \
  -e TZ=Europe/London \
  -p <HOST_PORT>:5444 \
  -v /path/to/images:/app/images \
  -v /path/to/data:/app/data \
  uping/boardgametracker:latest
```

---

## Configuration

### Environment Variables

| Variable          | Default          | Required | Description |
|-------------------|------------------|:--------:|-------------|
| `DB_HOST`         | -                | ✅       | PostgreSQL hostname |
| `DB_PORT`         | `5432`           | ❌       | PostgreSQL port |
| `DB_USER`         | -                | ✅       | PostgreSQL username |
| `DB_PASSWORD`     | -                | ✅       | PostgreSQL password |
| `DB_NAME`         | `boardgametracker` | ❌     | PostgreSQL database name |
| `STATISTICS`      | `0`              | ❌       | Enable/disable Sentry logging (0=off, 1=on) |
| `DATE_FORMAT`     | `yyyy-MM-dd`     | ❌       | Date format ([date-fns format](https://date-fns.org/v3.6.0/docs/format)) |
| `TIME_FORMAT`     | `HH:mm`          | ❌       | Time format ([date-fns format](https://date-fns.org/v3.6.0/docs/format)) |
| `TZ`              | `Europe/London`  | ❌       | Timezone (e.g., `America/New_York`, `Asia/Tokyo`) |
| `CURRENCY`        | `€`              | ❌       | Currency symbol for collection value tracking |

---

## Screenshots

<details>
<summary>Click to view screenshots</summary>

### Game Collection
<img src=".github/images/game-list.png" width="800" alt="Game list view">

### Game Details & Statistics
<img src=".github/images/game-details.png" width="800" alt="Game details page">

### Player Statistics
<img src=".github/images/user-details.png" width="800" alt="User details page">

### Session Recording
<img src=".github/images/new-session.png" width="800" alt="New session form">

</details>

---

## Technology Stack

### Backend
- .NET 8.0
- Entity Framework Core
- PostgreSQL
- Serilog for logging

### Frontend
- React 18
- TypeScript
- TanStack Router & Query
- Tailwind CSS
- Radix UI
- Vite

### Infrastructure
- Docker & Docker Compose
- GitHub Actions CI/CD
- SonarCloud code quality analysis

---

## Development

### Building from Source

1. Clone the repository:
```bash
git clone https://github.com/mregni/BoardGameTracker.git
cd BoardGameTracker
```

2. Run with Docker Compose:
```bash
docker-compose up --build
```

### Running Tests

**Backend:**
```bash
dotnet test
```

**Frontend:**
```bash
cd boardgametracker.client
npm test
```

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request


---

## Support

If you encounter any issues or have questions:
- Open an [issue](https://github.com/mregni/BoardGameTracker/issues)
- Check existing [discussions](https://github.com/mregni/BoardGameTracker/discussions)

---

## Acknowledgments

- Tested with [BrowserStack](https://www.browserstack.com/)
- Translations managed with [Crowdin](https://crowdin.com/)
- Game data from [BoardGameGeek](https://boardgamegeek.com/)
