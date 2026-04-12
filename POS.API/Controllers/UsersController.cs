using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Pos.BuisnessLayer;
using Pos.Datalayer.Dtos;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses;
using POS.Shared.Responses.DTOs;
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
                case OperationStatus.Success:
                    return Ok(new {Message=Result.Message, UserID= Result.Data});

                case OperationStatus.DuplicateRecord:
                    return Conflict(new { Message = Result.Message });
                case OperationStatus.InvalidData:
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
                case OperationStatus.Success:
                    return Ok(new { Message = result.Message, UserID = result.Data });

                case OperationStatus.InvalidData:
                    return BadRequest(new { Message = result.Message }); 

                case OperationStatus.NotFound:
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

            if (pagedList.Status != OperationStatus.Success || pagedList.Data == null)
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
                case OperationStatus.Success:
                    return Ok(new {Message=result.Message,userid=result.Data});

                case OperationStatus.NotFound:
                    return NotFound(new { Message = result.Message });

                default:
                    return StatusCode(500, new { Message = result.Message });

            }

        }
        [HttpGet("GetUserByName/{username}")] 
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)] 
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetUserByName (string username)
        {
            Log.Information("Controller: Getting user with ID {Id}", username);

            var result=await _userService.GetUserByUsername(username);
            if (result == null)
                return StatusCode(500, new { Message = "CONTROLLER : AN ERRORR OCCUERD ." });


            switch (result.Status)
            {
                case OperationStatus.Success:
                    return Ok(new { Message = result.Message, userid = result.Data });

                case OperationStatus.NotFound:
                    return NotFound(new { Message = result.Message });

                default:
                    return StatusCode(500, new { Message = result.Message });

            }
        }
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserResponseDto dto)
        {
            Log.Information("Controller: Attempting to update user {Id}", id);

            if (id != dto.Id)
            {
                Log.Warning("Controller: Update ID mismatch. Route ID: {RouteId}, Body ID: {BodyId}", id, dto.Id);
                return BadRequest(new { Message = "رقم المستخدم في الرابط لا يتطابق مع الرقم داخل البيانات المرسلة" });
            }

            var result = await _userService.UpdateUser(dto);

            if (result == null)
                return StatusCode(500, new { Message = "حدث خطأ داخلي في الخادم." });

            switch (result.Status)
            {
                case OperationStatus.Success:
                    return Ok(new { Message = result.Message, UserId = result.Data });

                case OperationStatus.NotFound:
                    return NotFound(new { Message = result.Message });

                case OperationStatus.DuplicateRecord:
                case OperationStatus.InvalidData:
                    return BadRequest(new { Message = result.Message });

                default:
                    return StatusCode(500, new { Message = result.Message });
            }
        }

        [HttpPut("ChangeUserPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task <IActionResult>ChangePassword(int id,[FromBody] ChangePasswordDto passwordDto)
        {
            Log.Information("Controller: Changing password for user ID {Id}", id);
            var result = await _userService.ChangeUserPassword(id, passwordDto.NewPassword);

          switch (result.Status)
           {
                case OperationStatus.Success:
                    return Ok(new { Message = result.Message, UserId = result.Data });

                case OperationStatus.NotFound:
                    return NotFound(new { Message = result.Message });

                case OperationStatus.DuplicateRecord:
                case OperationStatus.InvalidData:
                    return BadRequest(new { Message = result.Message });

                default:
                    return StatusCode(500, new { Message = result.Message });

            }






        }


    }
}
