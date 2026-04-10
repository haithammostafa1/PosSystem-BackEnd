using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Pos.BuisnessLayer;
using Pos.Datalayer.Dtos;
using POS.Shared.Responses;
using Serilog;

namespace POS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {

        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;

        }
        [HttpPut("AddNewUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddNewUser([FromBody]UserCreatedDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var Result= await _userService.AddNewUser(dto);

             switch (Result.Status)
            {
                case UserOperationResult.Success:
                    return Ok(new {Message=Result.Message, UserID= Result.Data});

                case UserOperationResult.DuplicateUsername:
                    return Conflict(new { Message = Result.Message });
                case UserOperationResult.InvalidData:
                    return BadRequest(new {Message= Result.Message});

                default:
                    return StatusCode(500, new { Message = Result.Message });

            }

        }
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult>DeleteUser(int id)
        {
            Log.Information("Controller: Deleting user ID {Id}", id);
            var result= await _userService.DeleteUser(id);

         switch (result.Status) 
        {
                case UserOperationResult.Success:
                    return Ok(new { Message = result.Message, UserID = result.Data });

                case UserOperationResult.InvalidData:
                    return BadRequest(new { Message = result.Message }); 

                case UserOperationResult.NotFound:
                    return NotFound(new { Message = result.Message });

                default:
                    return StatusCode(500, new { Message = result.Message });
            }

        }






    }
}
