
namespace Auth.Core.Aggregates.User
{
    public class Address
    {
        public int id {  get; set; } 
        public string? Street { get; set; } = string.Empty;  
        public string? City { get; set; } = string.Empty;    
        public string? State { get; set; }
        public string? ZipCode { get; set; } 
        public string? Country { get; set; }  
        public string? Government { get; set; }
        public string UserId { get; set; } = null!; 
        public User? User { get; set; }  
    }
}