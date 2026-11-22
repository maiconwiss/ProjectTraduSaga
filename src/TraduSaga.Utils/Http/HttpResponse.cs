using System;
using System.Net;

namespace TraduSaga.Utils.Http
{
    public class HttpResponse
    {
        public virtual string? Body { get; protected set; }

        public virtual bool IsSuccessful { get; protected set; }

        public virtual Exception? InnerException { get; protected set; }

        public virtual HttpStatusCode? StatusCode { get; protected set; }

        public HttpResponse()
        {
            IsSuccessful = false;
        }

        public HttpResponse(bool isSuccessful, string? body, Exception? exception)
            : this(isSuccessful, body, exception, null)
        {
        }

        public HttpResponse(bool isSuccessful, string? body, Exception? exception, HttpStatusCode? statusCode)
        {
            Body = body;
            IsSuccessful = isSuccessful;
            InnerException = exception;
            StatusCode = statusCode;
        }
    }
}
