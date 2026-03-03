---
layout: docs-base
title: Installation
permalink: /docs/installation/
---

# Installation

BoardGameTracker is distributed as a Docker image, making it easy to deploy on any server.

## Prerequisites

- Docker and Docker Compose installed on your server
- A PostgreSQL database (or use the included docker-compose setup)

## Quick Start with Docker Compose

1. Create a `docker-compose.yml` file:

   ```yaml
   version: "3.8"
   services:
     boardgametracker:
       image: boardgametracker:latest
       ports:
         - "8080:8080"
       environment:
         - ConnectionStrings__DefaultConnection=Host=db;Database=boardgametracker;Username=postgres;Password=yourpassword
       depends_on:
         - db

     db:
       image: postgres:16
       environment:
         - POSTGRES_DB=boardgametracker
         - POSTGRES_USER=postgres
         - POSTGRES_PASSWORD=yourpassword
       volumes:
         - pgdata:/var/lib/postgresql/data

   volumes:
     pgdata:
   ```

2. Start the application:

   ```bash
   docker-compose up -d
   ```

3. Open your browser and navigate to `http://localhost:8080`

## Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | Yes |

## Next Steps

Once installed, head over to the [Usage Guide](../usage/) to learn how to set up your collection.
