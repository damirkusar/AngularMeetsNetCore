﻿using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Identity.Interface;
using Identity.Interface.Constants;
using IdentityProvider.Attributes;
using IdentityProvider.Filters;
using IdentityProvider.TransferObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IdentityProvider.Controllers.Identity
{
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = false)]
    [Route("api/[controller]")]
    public class ChangePasswordController : Controller
    {
        private readonly ILogger<ChangePasswordController> logger;
        private readonly IMapper mapper;
        private readonly IChangeUserPasswordService changeUserPasswordService;


        public ChangePasswordController(
            ILogger<ChangePasswordController> logger,
            IMapper mapper,
            IChangeUserPasswordService changeUserPasswordService)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.changeUserPasswordService = changeUserPasswordService;
        }

        [HttpPost]
        [ValidateModelState]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(NoContentResult))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(ObjectResult))]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var result = await this.changeUserPasswordService.ChangePasswordAsync(this.User, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return this.StatusCode((int) HttpStatusCode.InternalServerError, result.Errors);
            }

            return this.NoContent();
        }

        [AuthorizeRoles(Roles.Admin)]
        [HttpPost]
        [Route("force")]
        [ValidateModelState]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(NoContentResult))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(ObjectResult))]
        public async Task<IActionResult> ForceChangePassword([FromBody] ForceChangePasswordRequest request)
        {
            var result = await this.changeUserPasswordService.ForceChangePasswordAsync(request.UserName, request.NewPassword);
            if (!result.Succeeded)
            {
                return this.StatusCode((int) HttpStatusCode.InternalServerError, result.Errors);
            }

            return this.NoContent();
        }
    }
}