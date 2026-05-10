using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Reservo.Attributes
{
    public class AuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string? _requiredRole;
        
        public AuthorizeAttribute(string? requiredRole = null)
        {
            _requiredRole = requiredRole;
        }
        
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var isLogged = context.HttpContext.Session.GetString("IsLogged");
            if (isLogged != "true")
            {
                context.Result = new RedirectToActionResult("Logowanie", "IO", null);
                return;
            }
            
            if (!string.IsNullOrEmpty(_requiredRole))
            {
                var userRole = context.HttpContext.Session.GetString("UserRole");
                if (userRole != _requiredRole)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
            
            base.OnActionExecuting(context);
        }
    }
}