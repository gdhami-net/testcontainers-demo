# testcontainers-demo

Companion repo for [Integration tests that lie](https://gdhami.net/blog/integration-tests-that-lie.html).

Two integration tests that run **real infrastructure** as throwaway Docker
containers owned by the test run — no mocks, no shared environment:

- `SqlDialectTests` — the same `NVARCHAR(10)` insert is **green on SQLite**
  and **refused by real SQL Server**. That gap is exactly what a "close
  enough" test database hides.
- `RabbitMqRoundTripTests` — a message round-trips through an actual
  RabbitMQ broker, not a list pretending to be one.

## Run it

Prerequisite: a Docker-compatible runtime (Docker Desktop, Podman, Rancher).

```bash
dotnet test
```

First run pulls the SQL Server and RabbitMQ images, so give it a few minutes;
afterwards the suite starts in seconds. Containers are created and destroyed
per test class — run it twice in parallel and they won't collide.

MIT licensed. Argue with it.
