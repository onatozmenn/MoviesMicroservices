using CORE.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movies.APP.Features.Movies;

namespace Movies.API.Controllers
{
    [Route("api/movies")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly ILogger<MoviesController> _logger;
        private readonly IMediator _mediator;

        public MoviesController(ILogger<MoviesController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] MovieQueryRequest request, CancellationToken cancellationToken)
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
                _logger.LogError(exception, "MoviesGet failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during MoviesGet."));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(new MovieQueryRequest(), cancellationToken);
                var item = await response.SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
                return item is not null ? Ok(item) : NoContent();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MoviesGetById failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during MoviesGetById."));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(MovieAddRequest request, CancellationToken cancellationToken)
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
                _logger.LogError(exception, "MoviesPost failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during MoviesPost."));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, MovieUpdateRequest request, CancellationToken cancellationToken)
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
                _logger.LogError(exception, "MoviesPut failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during MoviesPut."));
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(new MovieRemoveRequest { Id = id }, cancellationToken);
                if (response.IsSuccessful)
                    return Ok(response);

                ModelState.AddModelError(nameof(Delete), response.Message);
                return InvalidRequest();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MoviesDelete failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during MoviesDelete."));
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
