# Movies Microservices

CTIS 465 Phase 1 Movie Catalog, adapted from [the instructor's Users example](https://github.com/cagilalsac/UsersMicroservices). The project retains the instructor's CORE, APP, API, DbService and MediatR request/handler/controller structure.

## Run

Install the .NET 8 SDK. From the repository root:

```sh
dotnet restore MoviesMicroservices.sln
dotnet build MoviesMicroservices.sln
dotnet run --project Movies.API --launch-profile http
```

Open <http://localhost:5223/swagger>. In Rider, open `MoviesMicroservices.sln` and run the Movies.API HTTP profile.

Startup applies the committed EF Core migrations to `Movies.API/MoviesDB.db`. Database files are local and ignored by Git. A clean clone needs no supplied database or login. Authentication and Gateway belong to the final phase and are not included in this Phase 1 application.

For sample data, open **MoviesDb → GET /api/SeedDb → Try it out → Execute** in Swagger. On an empty catalog it adds *Interstellar*, its director Christopher Nolan, the Science Fiction and Adventure genres, and both links. An existing catalog is preserved. Run **Movies → GET /api/movies** to see the result.

To create your own catalog, create a Director and Genre first, then a Movie with that DirectorId, then a MovieGenre link. Requests are also available in `Movies.API/Movies.API.http`; replace the example IDs with IDs returned by POST.

## Structure

```text
CORE/                  Record, CommandResponse, Service and DbService
Movies.APP/Domain/     Four catalog entities, MoviesDb and design-time factory
Movies.APP/Features/   Request, response and handler classes by entity
Movies.APP/Migrations/ Committed migration history and model snapshot
Movies.API/            Controllers, Swagger and application configuration
Movies.Tests/          SQLite behavior and migration regression tests
```

## API

Each entity has GET list, GET detail, POST, PUT and DELETE operations. MovieGenre detail/update/delete use both IDs.

| Entity | Base route | Filters and related queries |
| --- | --- | --- |
| Movie | `/api/movies` | `?name=matrix`, `?releaseYear=1999` |
| Director | `/api/directors` | `?lastName=nolan`, `?isRetired=false`, `/{id}/movies` |
| Genre | `/api/genres` | `?name=drama`, `/{id}/movies` |
| MovieGenre | `/api/movie-genres` | `?movieId=1`, `?genreId=1`, `/{movieId}/{genreId}` |

The catalog has 20 CRUD operations, two related-movie queries and one optional seed operation. GET returns `200 OK` with results or `204 No Content` for an empty list or missing record. POST, PUT and DELETE return `200 OK` with a `CommandResponse` on success. Invalid input, missing write targets and business rules such as duplicate links return `400 Bad Request` with the error message in `CommandResponse.message`. PUT uses the ID in the URL. For a MovieGenre replacement, put the old pair in the URL and the new pair in the body.

## Data rules

- Required names are validated and trimmed. Movie titles allow up to 100 characters and genre names up to 25. Movie and Genre duplicate-name checks use exact, case-sensitive names, matching the existing instructor-style checks.
- Name searches are case-insensitive, escape punctuation and execute through the SQLite provider. Multiple filters are combined with AND.
- Movie TotalRevenue defaults to 0 and cannot be negative. ReleaseDate may be null; year filtering excludes null dates. DirectorId must identify an existing Director.
- MovieGenre uses `(MovieId, GenreId)` as its database primary key. Both records must exist. Replacement removes the old link and adds the new link in one SaveChanges call.
- Deleting a Movie or Genre removes its links, not the other principal entity. Genre removal reuses the instructor's RoleRemove pattern.
- A Director with movies cannot be deleted. The API returns `400 Bad Request` and the database foreign key enforces the same rule.

See [the ER diagram](docs/er-diagram.md).

## Database history and existing clones

The context and connection-string key are now `MoviesDb`; the default file is `MoviesDB.db`. The old UsersDB.db is not automatically opened or converted. Before pulling this cleanup into an older clone, keep a backup of any local database you need: the previously tracked sample DB/WAL/SHM files are no longer part of the repository.

Historical migrations retain their IDs and old Users table names so previous schemas remain recognizable. The last migration removes the old Users, UserRoles, Roles and Statuses tables, preserving valid Movie, Director, Genre and MovieGenre data. Applying it deliberately to a previous database removes the old demo user data; back up first. No old CLR entities or API endpoints are needed for that history.

An older database containing Movies without valid Directors is rejected by the Director FK migration, preserving its existing data. Use a separate fresh database for the demo, or map real Director records before importing old movie data. No fake Directors are generated.

For a separate database:

```sh
dotnet run --project Movies.API --launch-profile http -- --ConnectionStrings:MoviesDb="Data Source=/absolute/path/to/catalog.db"
```

For future migration work, coordinate one integration branch and use the matching EF tools:

```sh
dotnet tool install --global dotnet-ef --version 8.0.29
dotnet ef migrations add MigrationName --project Movies.APP --startup-project Movies.API
```

When applying migrations manually, supply an absolute connection string so the design-time factory and runtime working directories cannot select different files:

```sh
dotnet ef database update --project Movies.APP --startup-project Movies.API --connection "Data Source=/absolute/path/to/MoviesMicroservices/Movies.API/MoviesDB.db"
```

## Verify and demo

```sh
dotnet test MoviesMicroservices.sln
```

Tests cover CRUD behavior, filters, duplicates, relationship rules, rollback and fresh/existing database migrations using disposable SQLite databases. For the HTTP demo in Swagger:

1. Create a Director, three Genres and a Movie.
2. Link the Movie to two Genres; check both ID filters and each Genre's movie list.
3. Exercise the name/year/retired filters and a duplicate or invalid request.
4. Update a Movie and replace one MovieGenre pair with a different valid pair.
5. Confirm Director deletion is rejected while it owns movies. Delete a linked Genre and verify the Movie survives; then delete the Movie and Director.

For submission, send the repository link and the four team member names to the instructor by email, as described in the [Phase 1 announcement](https://moodle.bilkent.edu.tr/current/mod/forum/discuss.php?d=2677).
