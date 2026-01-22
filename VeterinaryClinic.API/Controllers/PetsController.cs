using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
 
    [Route("api/[controller]")]
    public class PetsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public PetsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return await ExecuteFunction(async () => 
                await _mediator.Send(new GetAllPetQuery())
            );
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePetModal model)
        {
            return await ExecuteFunction(async () => 
                await _mediator.Send(new CreatePetCommand(model))
            );
        }
    }   
}