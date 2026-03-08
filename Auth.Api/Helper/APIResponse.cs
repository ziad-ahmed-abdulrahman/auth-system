using Auth.Core.Aggregates.User;

namespace Auth.Api.Helper
{
    public class APIResponse<T> where T : class
    {
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        public APIResponse(int statuscode, string? message = null, T? data = default)
        {
            StatusCode = statuscode;
            IsSuccess = statuscode >= 200 && statuscode < 300;

            if (IsSuccess && data == null && message == null)
            {
                Message = "Operation successful, but no data to return.";
            }
            else
            {
                Message = message ?? GetMessageFromStatusCode(statuscode);
            }
            Data = data;
        }

        private string? GetMessageFromStatusCode(int statusCode)
        {
            return statusCode switch
            {
                200 => "Operation Successful",
                201 => "Resource Created Successfully",
                400 => "A bad request has been made",
                401 => "Authorized access is required",
                403 => "Forbidden: You don't have permission",
                404 => "Resource was not found",
                500 => "An internal server error occurred",
                _ => "An unexpected error occurred"
            };
        }
    }
}
