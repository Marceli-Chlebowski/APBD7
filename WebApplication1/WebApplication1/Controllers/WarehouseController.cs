using Microsoft.AspNetCore.Mvc;
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
    using (var connection = new SqlConnection(_connectionString))
    {
        connection.Open();
        using (var command = new SqlCommand("SELECT COUNT(*) FROM Product WHERE IdProduct = @ProductId", connection))
        {
            command.Parameters.AddWithValue("@ProductId", productId);
            int count = (int)command.ExecuteScalar();
            return count > 0;
        }
    }
}

private bool WarehouseExists(int warehouseId)
{
    using (var connection = new SqlConnection(_connectionString))
    {
        connection.Open();
        using (var command = new SqlCommand("SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @WarehouseId", connection))
        {
            command.Parameters.AddWithValue("@WarehouseId", warehouseId);
            int count = (int)command.ExecuteScalar();
            return count > 0;
        }
    }
}

private bool OrderExists(int productId, int amount, DateTime createdAt, SqlConnection connection)
{
    using (var command = new SqlCommand("SELECT COUNT(*) FROM [Order] WHERE IdProduct = @ProductId AND Amount = @Amount AND CreatedAt < @CreatedAt", connection))
    {
        command.Parameters.AddWithValue("@ProductId", productId);
        command.Parameters.AddWithValue("@Amount", amount);
        command.Parameters.AddWithValue("@CreatedAt", createdAt);
        int count = (int)command.ExecuteScalar();
        return count > 0;
    }
}

private void UpdateOrderFulfillment(int productId, int amount, DateTime createdAt, SqlConnection connection)
{
    using (var command = new SqlCommand("UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdProduct = @ProductId AND Amount = @Amount AND CreatedAt < @CreatedAt AND FulfilledAt IS NULL", connection))
    {
        command.Parameters.AddWithValue("@ProductId", productId);
        command.Parameters.AddWithValue("@Amount", amount);
        command.Parameters.AddWithValue("@CreatedAt", createdAt);
        command.ExecuteNonQuery();
    }
}

private int InsertProductWarehouse(WarehouseRequest request, SqlConnection connection)
{
    decimal price;
    using (var command = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @ProductId", connection))
    {
        command.Parameters.AddWithValue("@ProductId", request.IdProduct);
        price = (decimal)command.ExecuteScalar();
    }

    using (var command = new SqlCommand("INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt); SELECT SCOPE_IDENTITY();", connection))
    {
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdOrder", GetOrderId(request.IdProduct, request.Amount, request.CreatedAt, connection));
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@Price", price * request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);
        return Convert.ToInt32(command.ExecuteScalar());
    }
}

private int GetOrderId(int productId, int amount, DateTime createdAt, SqlConnection connection)
{
    using (var command = new SqlCommand("SELECT TOP 1 IdOrder FROM [Order] WHERE IdProduct = @ProductId AND Amount = @Amount AND CreatedAt < @CreatedAt AND FulfilledAt IS NULL ORDER BY CreatedAt ASC", connection))
    {
        command.Parameters.AddWithValue("@ProductId", productId);
        command.Parameters.AddWithValue("@Amount", amount);
        command.Parameters.AddWithValue("@CreatedAt", createdAt);
        return (int)command.ExecuteScalar();
    }
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