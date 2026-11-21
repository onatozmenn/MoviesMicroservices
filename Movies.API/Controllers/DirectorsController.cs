using CORE.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movies.APP.Features.Directors;

namespace Movies.API.Controllers
{
    [Route("api/directors")]
    [ApiController]
    public class DirectorsController : ControllerBase
    {
        private readonly ILogger<DirectorsController> _logger;
        private readonly IMediator _mediator;

        public DirectorsController(ILogger<DirectorsController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] DirectorQueryRequest request, CancellationToken cancellationToken)
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
                _logger.LogError(exception, "DirectorsGet failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during DirectorsGet."));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(new DirectorQueryRequest(), cancellationToken);
                var item = await response.SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
                return item is not null ? Ok(item) : NoContent();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "DirectorsGetById failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during DirectorsGetById."));
            }
        }

        [HttpGet("{id:int}/movies")]
        public async Task<IActionResult> GetMovies(int id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(
                    new DirectorMoviesQueryRequest { Id = id }, cancellationToken);
                var list = await response.ToListAsync(cancellationToken);
                return list.Count > 0 ? Ok(list) : NoContent();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "DirectorsGetMovies failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during DirectorsGetMovies."));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(DirectorAddRequest request, CancellationToken cancellationToken)
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
                _logger.LogError(exception, "DirectorsPost failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during DirectorsPost."));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, DirectorUpdateRequest request, CancellationToken cancellationToken)
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
                _logger.LogError(exception, "DirectorsPut failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during DirectorsPut."));
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(new DirectorRemoveRequest { Id = id }, cancellationToken);
                if (response.IsSuccessful)
                    return Ok(response);

                ModelState.AddModelError(nameof(Delete), response.Message);
                return InvalidRequest();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "DirectorsDelete failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during DirectorsDelete."));
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
