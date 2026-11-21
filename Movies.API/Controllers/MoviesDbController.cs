using Microsoft.AspNetCore.Mvc;
using Movies.APP.Domain;

namespace Movies.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesDbController : ControllerBase
    {
        [HttpGet, Route("~/api/SeedDb")]
        public IActionResult Seed([FromServices] MoviesDb db)
        {
            if (MoviesDbFactory.SeedDb(db))
                return Ok("Database seed successful.");

            return Ok("The catalog already contains data.");
        }
    }
}
