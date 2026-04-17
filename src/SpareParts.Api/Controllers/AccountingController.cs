using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Accounting;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/accounting")]
    [Authorize]
    public class AccountingController : SparePartsControllerBase
    {
        private readonly AccountingService _service;

        public AccountingController(AccountingService service)
        {
            _service = service;
        }

        [HttpGet("account-types")]
        public ActionResult<IEnumerable<AccountTypeDefinitionDto>> GetAccountTypes()
            => Ok(_service.GetAccountTypes());

        [HttpPost("account-types")]
        public ActionResult<string> CreateAccountType([FromBody] SaveAccountTypeRequest request)
            => Ok(_service.CreateAccountType(request));

        [HttpPut("account-types/{typeKey}")]
        public IActionResult UpdateAccountType(string typeKey, [FromBody] SaveAccountTypeRequest request)
        {
            _service.UpdateAccountType(typeKey, request);
            return NoContent();
        }

        [HttpDelete("account-types/{typeKey}")]
        public IActionResult DeleteAccountType(string typeKey)
        {
            _service.DeleteAccountType(typeKey);
            return NoContent();
        }

        [HttpGet("posting-roles")]
        public ActionResult<IEnumerable<PostingRoleDefinitionDto>> GetPostingRoles()
            => Ok(_service.GetPostingRoles());

        [HttpPost("posting-roles")]
        public ActionResult<string> CreatePostingRole([FromBody] SavePostingRoleRequest request)
            => Ok(_service.CreatePostingRole(request));

        [HttpPut("posting-roles/{roleKey}")]
        public IActionResult UpdatePostingRole(string roleKey, [FromBody] SavePostingRoleRequest request)
        {
            _service.UpdatePostingRole(roleKey, request);
            return NoContent();
        }

        [HttpDelete("posting-roles/{roleKey}")]
        public IActionResult DeletePostingRole(string roleKey)
        {
            _service.DeletePostingRole(roleKey);
            return NoContent();
        }

        [HttpGet("posting-settings")]
        public ActionResult<IEnumerable<PostingAccountSettingDto>> GetPostingSettings()
            => Ok(_service.GetPostingSettings());

        [HttpPut("posting-settings")]
        public IActionResult UpdatePostingSettings([FromBody] UpdatePostingSettingsRequest request)
        {
            _service.UpdatePostingSettings(request, CurrentUserId);
            return NoContent();
        }

        [HttpGet("journal-entries")]
        public ActionResult<IEnumerable<JournalEntrySummaryDto>> GetJournalEntries([FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
            => Ok(_service.GetJournalEntries(dateFrom, dateTo));

        [HttpGet("journal-entries/{id:int}")]
        public ActionResult<JournalEntryDetailDto> GetJournalEntry(int id)
            => Ok(_service.GetJournalEntry(id));

        [HttpPost("journal-entries/manual")]
        public ActionResult<int> CreateManualJournal([FromBody] CreateManualJournalEntryRequest request)
            => Ok(_service.CreateManualJournal(request, CurrentUserId));

        [HttpGet("ledger")]
        public ActionResult<LedgerReportDto> GetLedger([FromQuery] int accountId, [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
            => Ok(_service.GetLedger(accountId, dateFrom, dateTo));

        [HttpGet("trial-balance")]
        public ActionResult<TrialBalanceReportDto> GetTrialBalance([FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
            => Ok(_service.GetTrialBalance(dateFrom, dateTo));
    }
}
