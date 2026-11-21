using System.ComponentModel.DataAnnotations;
using CORE.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Movies.API.Controllers;
using Movies.APP.Domain;
using Movies.APP.Features.Directors;
using Movies.APP.Features.Genres;
using Movies.APP.Features.MovieGenres;
using Movies.APP.Features.Movies;
using Xunit;

namespace Movies.Tests
{
    public class CatalogContractTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly MoviesDb _db;
        private readonly ServiceProvider _services;
        private readonly IMediator _mediator;

        public CatalogContractTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _db = new MoviesDb(new DbContextOptionsBuilder<MoviesDb>().UseSqlite(_connection).Options);
            _db.Database.EnsureCreated();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<DbContext>(_db);
            services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(MovieAddHandler).Assembly));
            _services = services.BuildServiceProvider();
            _mediator = _services.GetRequiredService<IMediator>();
        }

        public void Dispose()
        {
            _services.Dispose();
            _db.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task Movies_controller_returns_no_content_for_empty_reads_and_bad_request_for_invalid_writes()
        {
            var controller = new MoviesController(NullLogger<MoviesController>.Instance, _mediator);
            Assert.IsType<NoContentResult>(await controller.Get(new MovieQueryRequest(), default));
            Assert.IsType<NoContentResult>(await controller.GetById(int.MaxValue, default));

            var badRequest = Assert.IsType<BadRequestObjectResult>(await controller.Post(
                new MovieAddRequest { Name = "Invalid", DirectorId = int.MaxValue }, default));
            var response = Assert.IsType<CommandResponse>(badRequest.Value);
            Assert.False(response.IsSuccessful);
            Assert.Equal("Director not found!", response.Message);
        }

        [Fact]
        public async Task Movie_genres_controller_returns_no_content_for_empty_reads_and_bad_request_for_invalid_writes()
        {
            var controller = new MovieGenresController(NullLogger<MovieGenresController>.Instance, _mediator);
            Assert.IsType<NoContentResult>(await controller.Get(new MovieGenreQueryRequest(), default));
            Assert.IsType<NoContentResult>(await controller.GetById(1, 1, default));

            var badRequest = Assert.IsType<BadRequestObjectResult>(await controller.Post(
                new MovieGenreAddRequest { MovieId = 1, GenreId = 1 }, default));
            var response = Assert.IsType<CommandResponse>(badRequest.Value);
            Assert.False(response.IsSuccessful);
            Assert.Equal("Movie not found!", response.Message);
        }

        [Fact]
        public async Task Catalog_queries_return_requested_order()
        {
            var director = new Director { FirstName = "Zoe", LastName = "Anderson" };
            var firstDirector = new Director { FirstName = "Ana", LastName = "Anderson" };
            var lastDirector = new Director { FirstName = "Bob", LastName = "Zimmer" };
            _db.Directors.AddRange(lastDirector, director, firstDirector);
            var lastMovie = new Movie { Name = "Z Film", Director = director };
            var firstMovie = new Movie { Name = "A Film", Director = director };
            _db.Movies.AddRange(lastMovie, firstMovie);
            var firstGenre = new Genre { Name = "Action" };
            var secondGenre = new Genre { Name = "Drama" };
            _db.Genres.AddRange(firstGenre, secondGenre);
            await _db.SaveChangesAsync();
            _db.MovieGenres.AddRange(
                new MovieGenre { Movie = firstMovie, Genre = firstGenre },
                new MovieGenre { Movie = lastMovie, Genre = secondGenre },
                new MovieGenre { Movie = lastMovie, Genre = firstGenre });
            await _db.SaveChangesAsync();

            var directors = await _mediator.Send(new DirectorQueryRequest());
            Assert.Equal(new[] { firstDirector.Id, director.Id, lastDirector.Id },
                await directors.Select(item => item.Id).ToListAsync());

            var movies = await _mediator.Send(new MovieQueryRequest());
            var directorMovies = await _mediator.Send(new DirectorMoviesQueryRequest { Id = director.Id });
            var genreMovies = await _mediator.Send(new GenreMoviesQueryRequest { Id = firstGenre.Id });
            foreach (var titles in new[] { movies, directorMovies, genreMovies })
            {
                Assert.Equal(new[] { firstMovie.Id, lastMovie.Id },
                    await titles.Select(item => item.Id).ToListAsync());
            }

            var links = await (await _mediator.Send(new MovieGenreQueryRequest())).ToListAsync();
            Assert.Equal(
                new[] { (lastMovie.Id, firstGenre.Id), (lastMovie.Id, secondGenre.Id), (firstMovie.Id, firstGenre.Id) },
                links.Select(item => (item.MovieId, item.GenreId)));
        }

        [Fact]
        public void Movie_and_genre_write_requests_reject_names_beyond_their_limits()
        {
            const string name = "Name";
            var overlongName = new string('A', 4096);
            object[] requests =
            {
                new MovieAddRequest { Name = overlongName, DirectorId = 1 },
                new MovieUpdateRequest { Name = overlongName, DirectorId = 1 },
                new GenreAddRequest { Name = overlongName },
                new GenreUpdateRequest { Name = overlongName }
            };

            foreach (var request in requests)
            {
                var context = new ValidationContext(request) { MemberName = name };
                var errors = new List<ValidationResult>();
                Assert.False(Validator.TryValidateProperty(overlongName, context, errors));
                Assert.Contains(name, Assert.Single(errors).MemberNames);
            }
        }
    }
}
