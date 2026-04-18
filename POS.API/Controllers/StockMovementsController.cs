using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.BuisnessLayer;
using POS.Shared.Responses.DTOs;
using Serilog;
using POS.Shared.Responses.Enums;

namespace POS.API.Controllers
{
    [Route("api/StockMovements")]
    [ApiController]
    public class StockMovementsController : ControllerBase
    {
        private readonly IStockMovementService _service;

        public StockMovementsController(IStockMovementService service)
        {
            _service = service;
        }

        // 🟢 Get by ProductId
        [HttpGet("GetByProductId/{productId}")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            Log.Information("Controller: Getting stock movements for product {Id}", productId);

            var result = await _service.GetByProductId(productId);

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

        // 🔴 (اختياري) Add Movement - غالبًا مش هتستخدمه
        [HttpPost("AddStockMovement")]
        public async Task<IActionResult> AddStockMovement([FromBody] StockMovementDto dto)
        {
            Log.Information("Controller: Adding stock movement for product {Id}", dto.ProductId);

            var result = await _service.AddStockMovement(dto);

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

