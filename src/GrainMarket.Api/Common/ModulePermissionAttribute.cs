using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Api.Common;

public enum PermissionAction { View, Create, Edit, Delete }

/// <summary>
/// Server-side enforcement of Setup > User Account role permissions per menu module. Applied on
/// controller actions; checks the caller's RolePermission row for the given module and action.
/// This is independent of, and in addition to, the protected-chart-of-accounts check in
/// ChartOfAccountService/LedgerQueryService.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ModulePermissionAttribute : Attribute, IAsyncActionFilter
{
    private readonly ModuleName _module;
    private readonly PermissionAction _action;

    public ModulePermissionAttribute(ModuleName module, PermissionAction action = PermissionAction.View)
    {
        _module = module;
        _action = action;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        if (!currentUser.IsAuthenticated || currentUser.RoleId is null)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
        var permission = await db.RolePermissions
            .FirstOrDefaultAsync(p => p.RoleId == currentUser.RoleId && p.Module == _module);

        var allowed = permission is not null && _action switch
        {
            PermissionAction.View => permission.CanView,
            PermissionAction.Create => permission.CanCreate,
            PermissionAction.Edit => permission.CanEdit,
            PermissionAction.Delete => permission.CanDelete,
            _ => false
        };

        if (!allowed)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.ForbidResult();
            return;
        }

        await next();
    }
}
