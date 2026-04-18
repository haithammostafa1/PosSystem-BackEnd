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
    [Route("api/Invoices")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }
        [HttpGet("GetInvoiceWithDetails/{id}")]
        public async Task<IActionResult> GetInvoiceWithDetails(int id)
        {
            Log.Information("Controller: Getting full invoice {Id}", id);

            var result = await _invoiceService.GetInvoiceById(id);

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
        [HttpPost("CreateFullInvoice")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateFullInvoice([FromBody] InvoiceCreateDto dto)
        {
            Log.Information("Controller: Creating full invoice {Number}", dto.InvoiceNumber);

            if (dto == null)
            {
                return BadRequest(new { message = "Invalid request data" });
            }

            var result = await _invoiceService.CreateInvoiceFullFlow(dto);

            return result.Status switch
            {
                OperationStatus.Success => CreatedAtAction(
                    nameof(GetInvoiceWithDetails), // 🔥 رجعنا الفاتورة الكاملة
                    new { id = result.Data },
                    new
                    {
                        message = result.Message,
                        invoiceId = result.Data
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
        [HttpGet("GetAllInvoicesPaged")]
        public async Task<ActionResult> GetAllInvoicesPaged([FromQuery] PaginationParams pagination)
        {
            Log.Information("Controller: Getting invoices page {Page}", pagination.PageNumber);

            var result = await _invoiceService.GetAllInvoicesPaged(pagination);

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

        [HttpGet("GetInvoiceById/{id}")]
        public async Task<ActionResult> GetInvoiceById(int id)
        {
            Log.Information("Controller: Getting invoice {Id}", id);

            if (id <= 0)
                return BadRequest(new { message = "Invalid invoice id" });

            var result = await _invoiceService.GetInvoiceById(id);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new { message = result.Message, data = result.Data }),
                OperationStatus.NotFound => NotFound(new { message = result.Message }),
                OperationStatus.InvalidData => BadRequest(new { message = result.Message }),
                _ => StatusCode(500, new { message = result.Message })
            };
        }

        [HttpGet("GetInvoiceByNumber/{invoiceNumber}")]
        public async Task<ActionResult> GetInvoiceByNumber(string invoiceNumber)
        {
            Log.Information("Controller: Getting invoice {Number}", invoiceNumber);

            var result = await _invoiceService.GetInvoiceByNumber(invoiceNumber);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new { message = result.Message, data = result.Data }),
                OperationStatus.NotFound => NotFound(new { message = result.Message }),
                OperationStatus.InvalidData => BadRequest(new { message = result.Message }),
                _ => StatusCode(500, new { message = result.Message })
            };
        }

        [HttpPost("AddNewInvoice")]
        public async Task<IActionResult> AddNewInvoice([FromBody] InvoiceCreateDto dto)
        {
            Log.Information("Controller: Creating invoice {Number}", dto.InvoiceNumber);

            var result = await _invoiceService.AddNewInvoice(dto);

            return result.Status switch
            {
                OperationStatus.Success => CreatedAtAction(
                    nameof(GetInvoiceById),
                    new { id = result.Data },
                    new { message = result.Message, invoiceId = result.Data }),

                OperationStatus.DuplicateRecord => Conflict(new { message = result.Message }),
                OperationStatus.InvalidData => BadRequest(new { message = result.Message }),
                _ => StatusCode(500, new { message = result.Message })
            };
        }

        [HttpPut("UpdateInvoice")]
        public async Task<ActionResult> UpdateInvoice([FromBody] InvoiceResponseDto dto)
        {
            Log.Information("Controller: Updating invoice {Id}", dto.Id);

            if (dto == null)
                return BadRequest(new { message = "Invalid data" });

            var result = await _invoiceService.UpdateInvoice(dto);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new { message = result.Message, invoiceId = result.Data }),
                OperationStatus.NotFound => NotFound(new { message = result.Message }),
                OperationStatus.DuplicateRecord => Conflict(new { message = result.Message }),
                OperationStatus.InvalidData => BadRequest(new { message = result.Message }),
                _ => StatusCode(500, new { message = result.Message })
            };
        }

        [HttpDelete("DeleteInvoice/{id}")]
        public async Task<ActionResult> DeleteInvoice(int id)
        {
            Log.Information("Controller: Deleting invoice {Id}", id);

            var result = await _invoiceService.DeleteInvoice(id);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new { message = result.Message, invoiceId = result.Data }),
                OperationStatus.NotFound => NotFound(new { message = result.Message }),
                OperationStatus.InvalidData => BadRequest(new { message = result.Message }),
                _ => StatusCode(500, new { message = result.Message })
            };
        }

        [HttpPost("RestoreInvoice/{id}")]
        public async Task<ActionResult> RestoreInvoice(int id)
        {
            Log.Information("Controller: Restoring invoice {Id}", id);

            var result = await _invoiceService.RestoreInvoice(id);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new { message = result.Message, invoiceId = result.Data }),
                OperationStatus.NotFound => NotFound(new { message = result.Message }),
                OperationStatus.InvalidData => BadRequest(new { message = result.Message }),
                _ => StatusCode(500, new { message = result.Message })
            };
        }

        [HttpGet("GetByCustomer/{customerId}")]
        public async Task<ActionResult> GetByCustomer(int customerId)
        {
            Log.Information("Controller: Getting invoices for customer {Id}", customerId);

            var result = await _invoiceService.GetInvoicesByCustomerId(customerId);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new { message = result.Message, data = result.Data }),
                OperationStatus.InvalidData => BadRequest(new { message = result.Message }),
                _ => StatusCode(500, new { message = result.Message })
            };
        }

        [HttpGet("GetTotalSales")]
        public async Task<ActionResult> GetTotalSales(DateTime from, DateTime to)
        {
            Log.Information("Controller: Getting sales from {From} to {To}", from, to);

            var result = await _invoiceService.GetTotalSales(from, to);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new { message = result.Message, total = result.Data }),
                OperationStatus.InvalidData => BadRequest(new { message = result.Message }),
                _ => StatusCode(500, new { message = result.Message })
            };
        }
    }
}

