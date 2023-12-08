namespace DotNetCoreSqlDb.Models
{
    public class Error
    {
        public string? RequestId { get; set; }

        public required string ErrorCode { get; set; }

        public required string ErrorMessage { get; set; }
    }
}