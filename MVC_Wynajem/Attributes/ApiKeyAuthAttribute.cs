using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Reservo.Models;

namespace Reservo.Attributes
{
    public class ApiKeyAuthAttribute : ActionFilterAttribute
    {
        private readonly string? _requiredRole;
        
        public ApiKeyAuthAttribute(string? requiredRole = null)
        {
            _requiredRole = requiredRole;
        }
        
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            
            // Sprawdź klucz API w nagłówku lub query string
            string? apiKey = context.HttpContext.Request.Headers["X-API-Key"].FirstOrDefault() ??
                           context.HttpContext.Request.Query["apiKey"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(apiKey))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Brak klucza API" });
                return;
            }
            
            var user = dbContext.Users.FirstOrDefault(u => u.ApiKey == apiKey);
            if (user == null)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Nieprawidłowy klucz API" });
                return;
            }
            
            // Sprawdź wymagane uprawnienia
            if (!string.IsNullOrEmpty(_requiredRole) && user.Role != _requiredRole)
            {
                context.Result = new ForbidResult();
                return;
            }
            
            // Dodaj informacje o użytkowniku do kontekstu
            context.HttpContext.Items["ApiUser"] = user;
            
            base.OnActionExecuting(context);
        }
    }
}