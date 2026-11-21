using CORE.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movies.APP.Features.Genres;

namespace Movies.API.Controllers
{
    [Route("api/genres")]
    [ApiController]
    public class GenresController : ControllerBase
    {
        private readonly ILogger<GenresController> _logger;
        private readonly IMediator _mediator;

        public GenresController(ILogger<GenresController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] GenreQueryRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                    return InvalidRequest();

                var response = await _mediator.Send(request, cancellationToken);
                var list = await response.ToListAsync(cancellationToken);
                return list.Count > 0 ? Ok(list) : NoContent();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "GenresGet failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during GenresGet."));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(new GenreQueryRequest(), cancellationToken);
                var item = await response.SingleOrDefaultAsync(g => g.Id == id, cancellationToken);
                return item is not null ? Ok(item) : NoContent();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "GenresGetById failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during GenresGetById."));
            }
        }

        [HttpGet("{id:int}/movies")]
        public async Task<IActionResult> GetMovies(int id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(
                    new GenreMoviesQueryRequest { Id = id }, cancellationToken);
                var list = await response.ToListAsync(cancellationToken);
                return list.Count > 0 ? Ok(list) : NoContent();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "GenresGetMovies failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during GenresGetMovies."));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(GenreAddRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                    return InvalidRequest();

                var response = await _mediator.Send(request, cancellationToken);
                if (response.IsSuccessful)
                    return Ok(response);

                ModelState.AddModelError(nameof(Post), response.Message);
                return InvalidRequest();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "GenresPost failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during GenresPost."));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, GenreUpdateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                    return InvalidRequest();

                request.Id = id;
                var response = await _mediator.Send(request, cancellationToken);
                if (response.IsSuccessful)
                    return Ok(response);

                ModelState.AddModelError(nameof(Put), response.Message);
                return InvalidRequest();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "GenresPut failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during GenresPut."));
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(new GenreRemoveRequest { Id = id }, cancellationToken);
                if (response.IsSuccessful)
                    return Ok(response);

                ModelState.AddModelError(nameof(Delete), response.Message);
                return InvalidRequest();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "GenresDelete failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during GenresDelete."));
            }
        }

        private BadRequestObjectResult InvalidRequest()
        {
            var messages = ModelState.Values
                .SelectMany(entry => entry.Errors)
                .Select(error => error.ErrorMessage);

            return BadRequest(new CommandResponse(false, string.Join("|", messages)));
        }
    }
}
