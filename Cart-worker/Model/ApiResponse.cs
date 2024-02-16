namespace Cart_worker.Model
{
    public class ApiResponse<T>
    {
        public ApiResponse(T data, int statusCode)
        {
            Data = data;
            StatusCode = statusCode;
        }

        public T Data { get; set; }
        public int StatusCode { get; set; }
    }
}
