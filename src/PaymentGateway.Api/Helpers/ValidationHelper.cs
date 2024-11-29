using FluentValidation.Results;

using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Helpers
{
    public static class ValidationHelper
    {
        /// <summary>
        /// Generates a nicely formatted validation error messages to return as part of an API response.
        /// </summary>
        /// <param name="validationResult">A validation result</param>
        /// <returns>An array of errors, each containing PropertyName and ErrorMessage</returns>
        public static FriendlyValidationError[] ApiFriendlyValidationErrors(ValidationResult validationResult)
        {
            return validationResult.Errors
                .Select(x => new FriendlyValidationError { PropertyName = x.PropertyName, ErrorMessage = x.ErrorMessage })
                .ToArray();
        }
    }
}
