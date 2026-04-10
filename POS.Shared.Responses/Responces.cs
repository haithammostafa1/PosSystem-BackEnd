namespace POS.Shared.Responses
{
    public enum UserOperationResult
    {
        Success = 1,
        NotFound = 0,
        DuplicateUsername = -1,
        InvalidData = -2,
        Failed = -99
    }

    public class OperationResult<T>
    {
        public UserOperationResult Status { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;

        public static OperationResult<T> SuccessResult(T Data, string Message = "Operation completed successfully")
        {

            return new OperationResult<T> { Status = UserOperationResult.Success, Data = Data, Message = Message };

        }
        //many type of failure 
        public static OperationResult<T> FailureResult(UserOperationResult status, string Message)
        {

            return new OperationResult<T> { Status = status, Data = default, Message = Message };

        }











       

    }
}
