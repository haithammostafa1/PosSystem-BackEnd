using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Pos.BuisnessLayer;
using Pos.Datalayer.Dtos;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses;
using Serilog;
using System.Text.Json;

namespace POS.API.Controllers
{
    [Route("api/Users")]
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

        [HttpGet("GetAllUsersPaged")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public  async Task<ActionResult<OperationResult<PagedList<UserResponseDto>>>> GetAllUsersPaged([FromQuery] PaginationParams pagination)
        {

            Log.Information("Controller: Getting all users, page {PageNumber}, size {PageSize}", pagination.PageNumber, pagination.PageSize);

            var pagedList= await _userService.GetAllUsersPaged(pagination);

            if (pagedList.Status != UserOperationResult.Success || pagedList.Data == null)
            {
                return BadRequest(new { Message = pagedList.Message });
            }

            var metaData = new
            {
                pagedList.Data.MetaData.TotalCount,
                pagedList.Data.MetaData.PageSize,
                pagedList.Data.MetaData.CurrentPage,
                pagedList.Data.MetaData.TotalPages,
                pagedList.Data.MetaData.HasNext,
                pagedList.Data.MetaData.HasPrevious

            };
            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(metaData));

            return Ok(pagedList);





        }
        [HttpGet("GetUserById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult>GetUserById(int id)
        {

           Log.Information("Controller: Getting user with ID {Id}", id);

            var result= await _userService.GetUserById(id);
            if (result == null)
                return StatusCode(500, new { Message = "CONTROLLER : AN ERRORR OCCUERD ." });
            
            
            switch (result.Status)
           {
                case UserOperationResult.Success:
                    return Ok(new {Message=result.Message,userid=result.Data});

                case UserOperationResult.NotFound:
                    return NotFound(new { Message = result.Message });

                default:
                    return StatusCode(500, new { Message = result.Message });

            }

        }



    }
}
