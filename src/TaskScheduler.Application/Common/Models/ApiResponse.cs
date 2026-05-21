using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Application.Common.Models
{
    public class ApiResponse<T>
    {
        public int Code { get; set; }

        public string Message { get; set; } = string.Empty;

        public T? Data { get; set; }

        public ApiResponse()
        {
        }

        public ApiResponse(int code, string message, T? data)
        {
            Code = code;
            Message = message;
            Data = data;
        }

        // Helper methods để tạo response dễ dàng
        public static ApiResponse<T> SuccessResponse(T data, string message = "Success", int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                Data = data,
                Message = message,
                Code = statusCode
            };
        }

        public static ApiResponse<T> FailureResponse(string message, int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                Data = default,
                Message = message,
                Code = statusCode
            };
        }
    }
}