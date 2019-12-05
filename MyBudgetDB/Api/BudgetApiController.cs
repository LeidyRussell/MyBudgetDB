using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyBudgetDB.Attributes;
using MyBudgetDB.Authorization;
using MyBudgetDB.Data;
using MyBudgetDB.Models.BudgetCommands;
using MyBudgetDB.Services;

namespace MyBudgetDB.Api
{
    [RequireHttps]
    [Route("api/BudgetApi"), FormatFilter]
    [FeatureEnabled(IsEnabled = true), 
     HandleException]
    [Authorize]
    public class BudgetApiController : Controller
    {
        private readonly BudgetService _budgetService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger _log;
        private readonly IAuthorizationService _authService;

        public BudgetApiController(
            BudgetService service, 
            ILogger<BudgetApiController> log,
            IAuthorizationService authService,
            UserManager<ApplicationUser> userManager)
        {
            _budgetService = service;
            _log = log;
            _userManager = userManager;
            _authService = authService;
        }

        [ResponseCache(NoStore = true)]
        [HttpGet("GetBudgets/{format}")]
        public async Task<IActionResult> GetBudgets()
        {
            var user = await _userManager.GetUserAsync(User);
            var budgets = _budgetService.GetBudgetsBrief(user.Id, User.HasClaim(c => c.Type == Claims.IsAdmin));
            return Ok(budgets);
        }

        [HttpGet("GetBy/{id}/{format}"), EnsureBudgetExist]
        public IActionResult GetById(int id)
        {
            var budget = _budgetService.GetBudget(id);
            return Ok(budget);
        }

        [HttpGet("GetUser/{format}")]
        public async Task<IActionResult> GetUserDetails()
        {
            var user = await _userManager.GetUserAsync(User);
            return Ok(user);
        }

        [ValidateModel, AddLastModifiedHeader]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateBudgetCommand cmd)
        {
            var user = await _userManager.GetUserAsync(User);
            var id = _budgetService.CreateBudget(cmd, user);

            return Ok(new { message = "Your budget id: " + id });
        }

        [EnsureBudgetExist, AddLastModifiedHeader, ValidateModel]
        [HttpPost("edit/{id}")] //need the id parameter to check EnsureBudgetExistAttribute
        public async Task<IActionResult> Edit(int id, [FromBody] UpdateBudgetCommand cmd)
        {
            var budget = _budgetService.GetBudget(id);
            var authResult = await _authService.AuthorizeAsync(User, budget, "CanViewBudget");
            if (!authResult.Succeeded)
            {
                return Forbid();
            }
            _budgetService.UpdateBudget(cmd);
            var newBudget = _budgetService.GetBudget(cmd.BudgetId);
            return Ok(newBudget);
        }

        [HttpDelete("delete/{id}"), EnsureBudgetExist, AddLastModifiedHeader]
        public async Task<IActionResult> Delete(int id)
        {
            var budget = _budgetService.GetBudget(id);
            var authResult = await _authService.AuthorizeAsync(User, budget, "CanViewBudget");
            if (!authResult.Succeeded)
            {
                return Forbid();
            }
            _budgetService.DeleteBudget(id);
            return Ok();
        }
    }
}
