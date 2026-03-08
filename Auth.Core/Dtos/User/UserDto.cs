namespace Auth.Core.Dtos.User
{
    public class UserDto
    {
        public string? Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public AddressDto? Address { get; set; }
    }
    public class UserForAdminDto : UserDto
    {
        public bool IsActive { get; set; }
   
    }
}
