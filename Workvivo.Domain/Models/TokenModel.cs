namespace Workvivo.Domain.Models
{
    public class TokenModel
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public Guid? UserId { get; set; }
        public bool? IsAdmin { get; set; }
        public string? UserType { get; set; }
        public string[] UserActions { get; set; }
    }
}
