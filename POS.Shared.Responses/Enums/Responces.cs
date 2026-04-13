namespace POS.Shared.Responses.Enums
{
    public enum OperationStatus
    {
        Success = 1,
        NotFound = 0,
        DuplicateRecord = -1,
        InsufficientStock = -3,
        InvalidData = -2,
        Failed = -99
    }
    public enum StockMovementType
    {
        Purchase = 1,      // وارد - فاتورة مشتريات (زيادة)
        Sale = 2,          // منصرف - فاتورة مبيعات (نقص)
        ReturnIn = 3,      // مرتجع من عميل (زيادة)
        ReturnOut = 4,     // مرتجع لمورد (نقص)
        Damage = 5,        // هالك أو تالف (نقص)
        Adjustment = 6     // تسوية جردية (قد تكون زيادة أو نقص)
    }
    public class OperationResult<T>
    {
        public OperationStatus Status { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;

        public static OperationResult<T> SuccessResult(T Data, string Message = "Operation completed successfully")
        {

            return new OperationResult<T> { Status = OperationStatus.Success, Data = Data, Message = Message };

        }
        //many type of failure 
        public static OperationResult<T> FailureResult(OperationStatus status, string Message)
        {

            return new OperationResult<T> { Status = status, Data = default, Message = Message };

        }











       

    }
}
