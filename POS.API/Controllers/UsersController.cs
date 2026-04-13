using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Pos.BuisnessLayer;
using Pos.Datalayer.Dtos;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses.DTOs;
using POS.Shared.Responses.Enums;
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
        [HttpPost("AddNewUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> AddNewUser([FromBody] UserCreatedDto dto)
        {
            Log.Information("Controller: Adding new user {Username}", dto.Username);

           
            if (dto == null)
            {
                return BadRequest(new { message = "Invalid request data" });
            }

            var result = await _userService.AddNewUser(dto);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
                {
                    message = result.Message,
                    userId = result.Data
                }),

                OperationStatus.DuplicateRecord => Conflict(new
                {
                    message = result.Message
                }),

                OperationStatus.InvalidData => BadRequest(new
                {
                    message = result.Message
                }),

                _ => StatusCode(500, new
                {
                    message = result.Message
                })
            };
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
        public async Task<ActionResult<OperationResult<PagedList<UserResponseDto>>>> GetAllUsersPaged([FromQuery] PaginationParams pagination)
        {
            Log.Information("Controller: Getting all users, page {PageNumber}, size {PageSize}",
                pagination.PageNumber, pagination.PageSize);

            var result = await _userService.GetAllUsersPaged(pagination);

            if (result.Status != OperationStatus.Success || result.Data == null)
            {
                return result.Status switch
                {
                    OperationStatus.NotFound => NotFound(result.Message),
                    OperationStatus.InvalidData => BadRequest(result.Message),
                    _ => StatusCode(500, result.Message)
                };
            }
            var metaData = new
            {
                result.Data.MetaData.TotalCount,
                result.Data.MetaData.PageSize,
                result.Data.MetaData.CurrentPage,
                result.Data.MetaData.TotalPages,
                result.Data.MetaData.HasNext,
                result.Data.MetaData.HasPrevious
            };


            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(metaData));

            return Ok(result);
        }
        [HttpGet("GetUserById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetUserById(int id)
        {
            Log.Information("Controller: Getting user with ID {Id}", id);

          
            if (id <= 0)
            {
                return BadRequest(new { Message = "Invalid user id" });
            }

            var result = await _userService.GetUserById(id);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
                {
                    message = result.Message,
                    data = result.Data
                }),

                OperationStatus.NotFound => NotFound(new
                {
                    message = result.Message
                }),

                OperationStatus.InvalidData => BadRequest(new
                {
                    message = result.Message
                }),

                _ => StatusCode(500, new
                {
                    message = result.Message
                })
            };
        }
        [HttpGet("GetUserByUsername")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetUserByUsername(string username)
        {
            Log.Information("Controller: Getting user by Username {Username}", username);

           
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new { message = "Username is required" });
            }

            var result = await _userService.GetUserByUsername(username);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
                {
                    message = result.Message,
                    data = new
                    {
                        result.Data.Id,
                        result.Data.Username,
                        result.Data.FullName,
                        result.Data.Role,
                        result.Data.IsActive
                    }
                }),

                OperationStatus.NotFound => NotFound(new
                {
                    message = result.Message
                }),

                OperationStatus.InvalidData => BadRequest(new
                {
                    message = result.Message
                }),

                _ => StatusCode(500, new
                {
                    message = result.Message
                })
            };
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
