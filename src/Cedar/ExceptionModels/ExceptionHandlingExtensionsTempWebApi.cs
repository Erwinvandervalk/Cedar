﻿namespace Cedar.ExceptionModels
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security;
    using System.Text;
    using System.Threading.Tasks;
    using Cedar.ExceptionModels.Client;
    using Cedar.Serialization;

    // The whole exception handling this will be changed in a seperate refactor, just implementing this to keep tests green
    
    internal static class ExceptionHandlingExtensionsTempWebApi
    {
        internal static async Task<HttpResponseMessage> ExecuteWithExceptionHandling_ThisIsToBeReplaced(
            this Func<Task> actionToRun,
            HandlerSettings handlerSettings)
        {
            Exception caughtException = null;
            try
            {
                await actionToRun();
                return null;
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            var aggregateException = caughtException as AggregateException;
            if(aggregateException != null)
            {
                caughtException = aggregateException.InnerExceptions.First();
            }

            var httpStatusException = caughtException as HttpStatusException;
            if (httpStatusException != null)
            {
                return HandleHttpStatusException(httpStatusException, handlerSettings);
            }
            var invalidOperationException = caughtException as InvalidOperationException;
            if (invalidOperationException != null)
            {
                return HandleBadRequest(invalidOperationException, handlerSettings);
            }
            var argumentException = caughtException as ArgumentException;
            if (argumentException != null)
            {
                return HandleBadRequest(argumentException, handlerSettings);
            }
            var formatException = caughtException as FormatException;
            if (formatException != null)
            {
                return HandleBadRequest(formatException, handlerSettings);
            }
            var securityException = caughtException as SecurityException;
            if (securityException != null)
            {
                return HandleBadRequest(securityException, handlerSettings);
            }
            return HandleInternalServerError(caughtException, handlerSettings);
        }

        private static HttpResponseMessage HandleBadRequest(InvalidOperationException ex, HandlerSettings options)
        {
            var exception = new HttpStatusException(ex.Message, HttpStatusCode.BadRequest, ex);

            return HandleHttpStatusException(exception, options);
        }

        private static HttpResponseMessage HandleBadRequest(ArgumentException ex, HandlerSettings options)
        {
            var exception = new HttpStatusException(ex.Message, HttpStatusCode.BadRequest, ex);

            return HandleHttpStatusException(exception, options);
        }

        private static HttpResponseMessage HandleBadRequest(FormatException ex, HandlerSettings options)
        {
            var exception = new HttpStatusException(ex.Message, HttpStatusCode.BadRequest, ex);

            return HandleHttpStatusException(exception, options);
        }

        private static HttpResponseMessage HandleBadRequest(SecurityException ex, HandlerSettings options)
        {
            var exception = new HttpStatusException(ex.Message, HttpStatusCode.Forbidden, ex);

            return HandleHttpStatusException(exception, options);
        }

        private static HttpResponseMessage HandleInternalServerError(Exception ex, HandlerSettings options)
        {
            var exception = new HttpStatusException(ex.Message, HttpStatusCode.InternalServerError, ex);

            return HandleHttpStatusException(exception, options);
        }

        private static HttpResponseMessage HandleHttpStatusException(
            HttpStatusException exception,
            HandlerSettings options,
            string contentType = "application/json")
        {
            var response = new HttpResponseMessage(exception.StatusCode);
            ExceptionModel exceptionModel = options.ExceptionToModelConverter.Convert(exception);
            string exceptionJson = options.Serializer.Serialize(exceptionModel);
            response.Content = new StringContent(exceptionJson, Encoding.UTF8, contentType);
            return response;
        }
    }
}
