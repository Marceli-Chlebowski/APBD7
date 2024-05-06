using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace WebApplication1.Controllers

{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";

        [HttpPost]
        public IActionResult AddProductToWarehouse([FromBody] WarehouseRequest request)
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                    return BadRequest("Invalid request format.");
                
                if (!ProductExists(request.IdProduct) || !WarehouseExists(request.IdWarehouse))
                    return NotFound("Product or warehouse not found.");
                
                if (request.Amount <= 0)
                    return BadRequest("Amount should be greater than 0.");
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    if (!OrderExists(request.IdProduct, request.Amount, request.CreatedAt, connection))
                        return NotFound("Order not found or already fulfilled.");
                    
                    UpdateOrderFulfillment(request.IdProduct, request.Amount, request.CreatedAt, connection);
                    
                    int productWarehouseId = InsertProductWarehouse(request, connection);

                    return Ok(productWarehouseId);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        
        [HttpPost("storedprocedure")]
        public IActionResult AddProductToWarehouseStoredProcedure([FromBody] WarehouseRequest request)
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                    return BadRequest("Invalid request format.");
                
                if (!ProductExists(request.IdProduct) || !WarehouseExists(request.IdWarehouse))
                    return NotFound("Product or warehouse not found.");
                
                if (request.Amount <= 0)
                    return BadRequest("Amount should be greater than 0.");
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    int productWarehouseId = ExecuteStoredProcedure(request, connection);

                    return Ok(productWarehouseId);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        
        private int ExecuteStoredProcedure(WarehouseRequest request, SqlConnection connection)
        {
            using (var command = new SqlCommand("NameOfYourStoredProcedure", connection))
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
                command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
                command.Parameters.AddWithValue("@Amount", request.Amount);
                command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);
                
                var productWarehouseIdParam = new SqlParameter("@ProductWarehouseId", System.Data.SqlDbType.Int);
                productWarehouseIdParam.Direction = System.Data.ParameterDirection.Output;
                command.Parameters.Add(productWarehouseIdParam);

                command.ExecuteNonQuery();
                
                int productWarehouseId = Convert.ToInt32(productWarehouseIdParam.Value);

                return productWarehouseId;
            }
        }



        private bool ProductExists(int productId)
        {
            // Implementation to check if product exists in the database
            throw new NotImplementedException();
        }

        private bool WarehouseExists(int warehouseId)
        {
            // Implementation to check if warehouse exists in the database
            throw new NotImplementedException();
        }

        private bool OrderExists(int productId, int amount, DateTime createdAt, SqlConnection connection)
        {
            // Implementation to check if order exists in the database
            throw new NotImplementedException();
        }

        private void UpdateOrderFulfillment(int productId, int amount, DateTime createdAt, SqlConnection connection)
        {
            // Implementation to update order fulfillment status
            throw new NotImplementedException();
        }

        private int InsertProductWarehouse(WarehouseRequest request, SqlConnection connection)
        {
            // Implementation to insert record into Product_Warehouse table
            throw new NotImplementedException();
        }
    }

    public class WarehouseRequest
    {
        public int IdProduct { get; set; }
        public int IdWarehouse { get; set; }
        public int Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}