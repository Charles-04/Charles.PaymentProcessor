using Charles.PaymentProcessor.Domain.DTOs;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Charles.PaymentProcessor.Api.Extension;

public static class ExceptionHandler
    {
        public static void ConfigureException(this IApplicationBuilder app, IWebHostEnvironment hostEnvironment)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.ContentType = "application/json";

                    IExceptionHandlerFeature exceptionHandleFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (exceptionHandleFeature != null)
                    {
                        switch (exceptionHandleFeature.Error)
                        {
                            //More Exceptions can be added as they are identified, those that arent identified will default to the 500 status code 
                            case InvalidDataException:
                            case InvalidOperationException:
                            case KeyNotFoundException:
                            case ArgumentException:
                                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                                break;
                            case DbUpdateException:
                                context.Response.StatusCode = StatusCodes.Status409Conflict;
                                break;
                            case UnauthorizedAccessException:
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                break;
                            case OperationCanceledException:
                                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                break;
                            default:
                                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                                break;
                        }

                        ErrorResponse err = new() { Success = false, Status = context.Response.StatusCode };
                        err.Message = hostEnvironment.IsProduction() && context.Response.StatusCode ==
                            StatusCodes.Status500InternalServerError
                                //? "We currently cannot complete this request process. Please retry or contact our support"
                                ? exceptionHandleFeature.Error.Message
                                : exceptionHandleFeature.Error.Message;

                        JsonSerializerSettings serializerSettings = new JsonSerializerSettings();
                        serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                        string msg = JsonConvert.SerializeObject(err, serializerSettings);
                        await context.Response.WriteAsync(msg);
                    }
                });
            });
        }
    }