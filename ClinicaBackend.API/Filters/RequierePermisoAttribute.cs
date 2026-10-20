using ClinicaBackend.API.Extensions;
using ClinicaBackend.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClinicaBackend.API.Filters
{
    public class RequierePermisoAttribute : TypeFilterAttribute
    {
        public RequierePermisoAttribute(string moduloCodigo, string accion)
            : base(typeof(RequierePermisoFilter))
        {
            Arguments = new object[] { moduloCodigo, accion };
        }
    }

    public class RequierePermisoFilter : IAsyncActionFilter
    {
        private readonly PermisosService _permisos;
        private readonly string _moduloCodigo;
        private readonly string _accion;

        public RequierePermisoFilter(PermisosService permisos, string moduloCodigo, string accion)
        {
            _permisos = permisos;
            _moduloCodigo = moduloCodigo;
            _accion = accion;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var idUsuario = context.HttpContext.User.ObtenerIdUsuario();
            if (idUsuario == null)
            {
                context.Result = new UnauthorizedObjectResult(new { mensaje = "Sesión no válida. Inicie sesión nuevamente." });
                return;
            }

            if (!await _permisos.TienePermisoAsync(idUsuario.Value, _moduloCodigo, _accion))
            {
                context.Result = new ObjectResult(new
                {
                    mensaje = $"No tiene permiso para {_accion.ToLowerInvariant()} en el módulo {_moduloCodigo}."
                })
                { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }

            await next();
        }
    }
}