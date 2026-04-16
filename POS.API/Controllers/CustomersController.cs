using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.BuisnessLayer;
using Pos.Datalayer.Helpers;
using POS.Shared.Responses.DTOs;
using Serilog;
using System.Text.Json;
using POS.Shared.Responses.Enums;

namespace POS.API.Controllers
{
    [Route("api/Customers")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet("GetAllCustomersPaged")]
        public async Task<ActionResult> GetAllCustomersPaged([FromQuery] PaginationParams pagination)
        {
            Log.Information("Controller: Getting customers page {Page}", pagination.PageNumber);

            var result = await _customerService.GetAllCustomersPaged(pagination);

            if (result.Status != OperationStatus.Success || result.Data == null)
            {
                return BadRequest(new { message = result.Message });
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

        [HttpGet("GetCustomerById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetCustomerById(int id)
        {
            Log.Information("Controller: Getting customer with ID {Id}", id);

            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid customer id" });
            }

            var result = await _customerService.GetCustomerById(id);

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

        [HttpPost("AddNewCustomer")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddNewCustomer([FromBody] CustomerSaveDto dto)
        {
            Log.Information("Controller: Request to add new customer {Name}", dto.Name);

            var result = await _customerService.AddNewCustomer(dto);

            return result.Status switch
            {
                OperationStatus.Success => CreatedAtAction(
                    nameof(GetCustomerById),
                    new { id = result.Data },
                    new { Message = result.Message, CustomerId = result.Data }),

                OperationStatus.DuplicateRecord => Conflict(new
                {
                    Message = result.Message
                }),

                OperationStatus.InvalidData => BadRequest(new
                {
                    Message = result.Message
                }),

                _ => StatusCode(500, new
                {
                    Message = result.Message
                })
            };
        }

        [HttpPut("UpdateCustomer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> UpdateCustomer([FromBody] CustomerSaveDto dto)
        {
            Log.Information("Controller: Updating customer {Id}", dto.Id);

            if (dto == null)
            {
                return BadRequest(new { message = "Invalid request data" });
            }

            var result = await _customerService.UpdateCustomer(dto);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
                {
                    message = result.Message,
                    customerId = result.Data
                }),

                OperationStatus.NotFound => NotFound(new
                {
                    message = result.Message
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

        [HttpDelete("DeleteCustomer/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> DeleteCustomer(int id)
        {
            Log.Information("Controller: Deleting customer {Id}", id);

            var result = await _customerService.DeleteCustomer(id);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
                {
                    message = result.Message,
                    customerId = result.Data
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
    }
}
