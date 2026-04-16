using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.BuisnessLayer;
using POS.Shared.Responses.DTOs;
using Serilog;
using POS.Shared.Responses.Enums;

namespace POS.API.Controllers
{
    [Route("api/InvoiceDetails")]
    [ApiController]
    public class InvoiceDetailsController : ControllerBase
    {
        private readonly IInvoiceDetailsService _service;

        public InvoiceDetailsController(IInvoiceDetailsService service)
        {
            _service = service;
        }

        [HttpPost("AddInvoiceDetails")]
        public async Task<IActionResult> AddInvoiceDetails([FromBody] List<InvoiceDetailSaveDto> details)
        {
            Log.Information("Controller: Adding invoice details");

            var result = await _service.AddInvoiceDetails(details);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
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

        [HttpGet("GetByInvoiceId/{invoiceId}")]
        public async Task<IActionResult> GetByInvoiceId(int invoiceId)
        {
            Log.Information("Controller: Getting invoice details {Id}", invoiceId);

            var result = await _service.GetByInvoiceId(invoiceId);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
                {
                    message = result.Message,
                    data = result.Data
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

        [HttpDelete("DeleteByInvoiceId/{invoiceId}")]
        public async Task<IActionResult> DeleteByInvoiceId(int invoiceId)
        {
            Log.Information("Controller: Deleting invoice details {Id}", invoiceId);

            var result = await _service.DeleteByInvoiceId(invoiceId);

            return result.Status switch
            {
                OperationStatus.Success => Ok(new
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
