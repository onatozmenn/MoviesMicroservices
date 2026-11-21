using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Movies.APP.Domain
{
    public class MoviesDbFactory : IDesignTimeDbContextFactory<MoviesDb>
    {
        public MoviesDb CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MoviesDb>();
            optionsBuilder.UseSqlite("Data Source=MoviesDB.db");
            return new MoviesDb(optionsBuilder.Options);
        }

        public static bool SeedDb(MoviesDb db)
        {
            using var transaction = db.Database.BeginTransaction();
            if (db.Directors.Any() || db.Genres.Any() || db.Movies.Any())
                return false;

            db.Movies.Add(new Movie
            {
                Name = "Interstellar",
                ReleaseDate = new DateTime(2014, 11, 7),
                Director = new Director { FirstName = "Christopher", LastName = "Nolan" },
                MovieGenres = new List<MovieGenre>
                {
                    new MovieGenre { Genre = new Genre { Name = "Science Fiction" } },
                    new MovieGenre { Genre = new Genre { Name = "Adventure" } }
                }
            });
            db.SaveChanges();
            transaction.Commit();
            return true;
        }
    }
}
