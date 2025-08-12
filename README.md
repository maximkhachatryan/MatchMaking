# MatchMaking

## Overview

This repository implements a matchmaking system for a game, developed as an interview test task. It groups players into matches using two .NET 9 applications:

- **MatchMaking.Service**: HTTP API for match search and retrieval.
- **MatchMaking.Worker**: Processes matchmaking requests via Kafka, forming matches with a configurable number of users (default: 3).

The solution uses Kafka for messaging, Redis for storage, and Docker Compose for deployment. Submitted as a Pull Request from `feature/initial` to `main`.

## Task Summary

- **Service**: HTTP endpoints:
    - POST `/api/matchmaking/search/{userId}`: Returns 204 (or 400)
    - GET `/api/matchmaking/matchinfo/userId/{userId}`: Returns 200 with `{ "matchId": string, "userIds": [] }`, or 404/400.
- **Worker**: Consumes `matchmaking.request`, forms matches, produces to `matchmaking.complete`.
- **Infrastructure**: Docker Compose with 1 service, 2 workers, Kafka, Redis, Zookeeper, KafkaUI, RedisCommander.
- **Code Style**: Records, nullable enabled, logging for successes/failures.

## Prerequisites

- Docker and Docker Compose.
- .NET 9 SDK (optional for local builds).

## Installation

1. Clone the repo:

   ```
   git clone https://github.com/maximkhachatryan/MatchMaking.git
   cd MatchMaking
   ```

## How to Run

1. Start with Docker Compose:

   ```
   docker-compose up -d
   ```

    - Exposes MatchMaking.Service on port 5000 (configurable).
    - Runs Kafka, Redis, 1 service, 2 workers,, Zookeeper, KafkaUI, RedisCommander

2. Test API:

    - POST `/api/matchmaking/search/{userId}`
    - GET `/api/matchmaking/info/userId/{userId}`
    - Use curl/Postman for testing.

3. Stop:

   ```
   docker-compose down
   ```

## Configuration

- **appsettings.json** (Worker): Set `MatchSize`(default: 3), Kafka/Redis connections.
- **docker-compose.yml**: Customize ports, replicas.

## Troubleshooting

- Check logs: `docker logs <container_name>`.
- Verify Kafka topics and Redis connectivity.

## License

For interview purposes only, not for production use.