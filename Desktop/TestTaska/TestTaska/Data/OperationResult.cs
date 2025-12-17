using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestTaska.Data
{
    public class OperationResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        protected OperationResult(bool isSuccess, string errorMessage = null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static OperationResult Success() => new OperationResult(true);
        public static OperationResult Failure(string message) => new OperationResult(false, message);
    }

    public class OperationResult<T> : OperationResult
    {
        public T Data { get; }

        private OperationResult(T data, bool isSuccess, string errorMessage) : base(isSuccess, errorMessage)
        {
            Data = data;
        }

        public static new OperationResult<T> Success(T data) => new OperationResult<T>(data, true, null);
        public static new OperationResult<T> Failure(string message) => new OperationResult<T>(default(T), false, message);
    }
}

