using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Shared
{
    /// <summary>
    /// Paged result.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public record PagedResult<T>
    {
        /// <summary>
        /// Page number.
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Page size.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total count.
        /// </summary>
        public int TotalCount { get; set; }

        public T Data { get; set; } = default!;

        public PagedResult(T data, int totalCount, int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            Data = data;
        }
    }

    public class PagedResponseDto<T> : ResponseDto<T>
    {
        /// <summary>
        /// Page number.
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Page size.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total count.
        /// </summary>
        public int TotalCount { get; set; }

        public PagedResponseDto(T data, int totalCount, int pageNumber, int pageSize)
        {
            this.PageNumber = pageNumber;
            this.PageSize = pageSize;
            this.TotalCount = totalCount;
            this.Data = data;
            this.Message = null!;
            this.Succeeded = true;
            this.Errors = null!;
        }
    }

    public class ResponseDto<T>
    {
        public ResponseDto() { }

        public ResponseDto(T data, string message = null!)
        {
            Succeeded = true;
            this.Message = message;
            this.Data = data;
        }

        public ResponseDto(string message)
        {
            Succeeded = false;
            this.Message = message;
        }

        /// <summary>
        /// Success status.
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// Response message.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Error list.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Response data.
        /// </summary>
        public T Data { get; set; } = default!;
    }


    //public class ApiResponse<T> where T : class
    //{
    //    [Required]
    //    public bool Success { get; set; }
    //    public string Message { get; set; }
    //    public T Data { get; set; }
    //    public object Errors { get; set; }

    //    public static ApiResponse<T> SuccessResponse(T data, string message = null)
    //    {
    //        return new ApiResponse<T> { Success = true, Message = message, Data = data };
    //    }

    //    public static ApiResponse<T> ErrorResponse(string message, object errors = null)
    //    {
    //        return new ApiResponse<T> { Success = false, Message = message, Errors = errors };
    //    }
    //}
}
