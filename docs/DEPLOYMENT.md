# Deploying Workvivo

## Running the whole thing locally

```bash
export MSSQL_SA_PASSWORD='a-strong-password-you-choose'
export JWT_SIGNING_KEY='at-least-32-characters-of-random-secret'
export ANONYMITY_KEY='a-different-32-plus-character-secret'

docker compose up --build
```

The site is then on <http://localhost:8080>. The API is not published to the host:
nginx proxies `/api` and `/hubs` to it over the compose network, so the browser
talks to one origin and the refresh cookie's `Path` and `SameSite` rules hold.

## Secrets

Nothing has a default. The application refuses to start without these, which is
deliberate - a platform that boots with a guessable signing key is worse than one
that does not boot.

| Variable | Why it matters if it leaks or is weak |
|---|---|
| `JWT_SIGNING_KEY` | Anyone who has it can mint a token for any user, including an administrator. Minimum 32 characters, enforced at startup. |
| `ANONYMITY_KEY` | Every anonymous poll and survey response becomes reversible by anybody who can also list the employees. Minimum 32 characters, enforced at startup. |
| `MSSQL_SA_PASSWORD` | The database. |
| `EMAIL_PASSWORD` | Outbound mail as the organisation. |

None of them belongs in `appsettings.json`, in the compose file, or in an image.
In production they come from the platform's secret store - Key Vault, Secrets
Manager, a Kubernetes `Secret` mounted as environment variables.

**One credential in this repository's history needs rotating**: an SMTP password
was committed in `appsettings.json` early in the project. It has been removed from
the working tree, but removing a secret from a file does not remove it from git
history, and it must be treated as disclosed.

## What this compose file is not

It runs the product; it is not a production topology.

- **The database is a container** with a password in an environment variable and a
  local volume. Production means a managed instance with its own backups, and a
  connection string from the secret store.
- **There is no TLS.** Terminate it at an ingress or load balancer in front of
  `web`. The API sets HSTS in production and expects to be reached over HTTPS.
- **Uploads are on a local volume.** Fine for one host; with more than one API
  replica, set `Storage__Provider` to a cloud provider so every instance reads the
  same files.
- **Hangfire runs in the API process.** With several replicas they will all pull
  from the same queue, which works - but the recurring jobs are registered by each
  of them, and a long job will block one instance's workers.

## Scaling past one instance

Two things need attention before the second replica:

1. **`ConnectionStrings__Redis`.** Without it, the SignalR backplane is
   in-process: a notification pushed by the instance that handled the write reaches
   only the clients connected to that instance. Everybody else sees it on their
   next fetch, so nothing breaks visibly - which is exactly why it is easy to miss.
   The distributed cache falls back the same way.
2. **File storage.** As above.

## Migrations

The API does not migrate on start. Applying schema changes automatically at
boot means several replicas racing each other, and a bad migration taking the
application down with it rather than failing on its own.

```bash
dotnet ef database update --project Workvivo.Infrastructure --startup-project Workvivo.API
```

Run it as a deployment step, before the new image is rolled out.

## The development seeder

`DevelopmentDataSeeder` creates sample employees with one shared, published
password. It refuses to run unless `ASPNETCORE_ENVIRONMENT` is `Development`, and
`Program.cs` checks the same thing again before calling it. Two independent checks
is the right number for something that creates accounts.

## The job dashboard

Hangfire's dashboard is mounted at `/jobs` **in development only**, behind a
loopback filter. It is not exposed elsewhere on purpose: it lets anyone who reaches
it requeue and delete jobs, and its authorisation filter runs against a browser
navigation, which carries no bearer token because the SPA holds the token in
memory. Operating it in production means a cookie scheme scoped to that path, or
reaching it through an authenticated proxy - a deliberate decision, not a default.
