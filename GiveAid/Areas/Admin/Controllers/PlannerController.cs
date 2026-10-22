using System.Text.Json;
using System.Text.Json.Nodes;
using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models;
using GiveAid.Models.ViewModels;
using GiveAid.Services.Planner;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>
    /// AI Programme Planner: the admin types a goal, the planner gathers facts and proposes a
    /// draft programme and volunteer invitations, and the admin approves, edits or rejects each one.
    /// </summary>
    public class PlannerController : AdminBaseController
    {
        private readonly PlannerService _planner;
        private readonly PlannerToolbox _tools;
        private readonly GiveAidContext _db;

        public PlannerController(PlannerService planner, PlannerToolbox tools, GiveAidContext db)
        {
            _planner = planner;
            _tools = tools;
            _db = db;
        }

        private string AdminName => string.IsNullOrEmpty(User.GetDisplayName()) ? "Admin" : User.GetDisplayName();

        // GET: /Admin/Planner
        public async Task<IActionResult> Index() => View(await BuildIndexAsync(null));

        private async Task<PlannerIndexViewModel> BuildIndexAsync(string? goal) => new()
        {
            Goal = goal ?? "",
            Mode = _planner.ActiveMode,
            ModeNote = _planner.ModeNote,
            Sessions = await _db.PlannerSessions.OrderByDescending(s => s.CreatedAt).Take(20).ToListAsync()
        };

        // POST: /Admin/Planner/Create  - plan a new goal
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Goal")] PlannerIndexViewModel model)
        {
            if (!ModelState.IsValid) return View(nameof(Index), await BuildIndexAsync(model.Goal));
            var session = await _planner.PlanAsync(model.Goal, AdminName, HttpContext.RequestAborted);
            if (session.Status == "Failed") Error("The plan could not be finished — see the message below.");
            return RedirectToAction(nameof(Details), new { id = session.Id });
        }

        // GET: /Admin/Planner/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var session = await _planner.GetSessionAsync(id);
            if (session == null) return NotFound();
            var steps = await _planner.GetStepsAsync(id);

            var vm = new PlannerDetailsViewModel
            {
                Session = session,
                Steps = steps,
                Programme = session.ProgrammeId != null ? await _db.Programmes.FindAsync(session.ProgrammeId.Value) : null,
                NgoNames = await _db.Ngos.ToDictionaryAsync(n => n.Id, n => n.Name)
            };

            // names + reasons for the volunteers in the invitation step (from the find_volunteers result)
            var found = steps.LastOrDefault(s => s.Tool == PlannerService.ToolVolunteers)?.ResultJson;
            if (found != null && JsonNode.Parse(found)?["volunteers"] is JsonArray list)
                foreach (var v in list)
                    vm.Volunteers[v!["volunteer_id"]!.GetValue<string>()] =
                        (v["name"]!.GetValue<string>(), v["city"]?.GetValue<string>(), v["reason"]?.GetValue<string>() ?? "");
            foreach (var step in steps.Where(s => s.Tool == PlannerService.ToolInvites))
                foreach (var key in PlannerService.Read<InviteProposal>(step)?.VolunteerIds ?? new())
                    if (!vm.Volunteers.ContainsKey(key) && await _tools.ResolveVolunteerAsync(key) is { } who)
                        vm.Volunteers[key] = (who.Name, null, "");
            return View(vm);
        }

        // GET: /Admin/Planner/EditDraft/12  (step id)
        public async Task<IActionResult> EditDraft(int id)
        {
            var step = await _db.PlannerSteps.FindAsync(id);
            if (step == null || step.Tool != PlannerService.ToolDraft) return NotFound();
            if (!PlannerService.IsOpen(step)) { Error("This proposal has already been decided."); return RedirectToAction(nameof(Details), new { id = step.SessionId }); }

            var d = PlannerService.Read<DraftProposal>(step)!;
            var vm = new DraftEditViewModel
            {
                StepId = step.Id, SessionId = step.SessionId, Title = d.Title, Category = d.Category, NgoId = d.NgoId, Venue = d.Venue,
                StartDate = d.StartDate, EndDate = d.EndDate, Beneficiaries = d.Beneficiaries, Summary = d.Summary, Details = d.Details,
                Lines = d.BudgetLines.ToList()
            };
            await FillDraftListsAsync();
            return View(PadLines(vm));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDraft(DraftEditViewModel vm)
        {
            var step = await _db.PlannerSteps.FindAsync(vm.StepId);
            if (step == null || step.Tool != PlannerService.ToolDraft) return NotFound();

            var edited = new DraftProposal
            {
                Title = vm.Title, Category = vm.Category, NgoId = vm.NgoId, Venue = vm.Venue, StartDate = vm.StartDate, EndDate = vm.EndDate,
                Beneficiaries = vm.Beneficiaries, Summary = vm.Summary, Details = vm.Details,
                BudgetLines = vm.Lines.Where(l => !string.IsNullOrWhiteSpace(l.Item)).ToList()
            };
            // empty budget rows are simply dropped, so ignore "amount is required" errors on them
            for (int i = 0; i < vm.Lines.Count; i++)
                if (string.IsNullOrWhiteSpace(vm.Lines[i].Item))
                {
                    ModelState.Remove($"Lines[{i}].Item");
                    ModelState.Remove($"Lines[{i}].Amount");
                }
            if (ModelState.IsValid)
            {
                var errors = await _planner.UpdateDraftAsync(step, edited, AdminName);
                if (errors.Count == 0)
                {
                    Success("Draft updated. It still needs your approval.");
                    return RedirectToAction(nameof(Details), new { id = step.SessionId });
                }
                foreach (var e in errors) ModelState.AddModelError("", char.ToUpper(e[0]) + e[1..] + ".");
            }
            await FillDraftListsAsync();
            vm.SessionId = step.SessionId;
            return View(PadLines(vm));
        }

        private static DraftEditViewModel PadLines(DraftEditViewModel vm)
        {
            int target = Math.Min(20, Math.Max(3, vm.Lines.Count + 2));     // a couple of empty rows to add items
            while (vm.Lines.Count < target) vm.Lines.Add(new BudgetLine());
            return vm;
        }

        private async Task FillDraftListsAsync()
        {
            ViewBag.Ngos = new SelectList(await _db.Ngos.OrderBy(n => n.Name).ToListAsync(), "Id", "Name");
            ViewBag.Categories = new SelectList(PlannerToolbox.Categories);
        }

        // POST: /Admin/Planner/Approve/12
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, List<string>? volunteerIds, string? message)
        {
            var step = await _db.PlannerSteps.FindAsync(id);
            if (step == null) return NotFound();

            InviteProposal? over = null;
            if (step.Tool == PlannerService.ToolInvites)
                over = new InviteProposal { VolunteerIds = volunteerIds ?? new(), Message = message ?? "" };

            var (ok, msg) = await _planner.ApproveAsync(step, AdminName, over,
                pid => Url.Action("Details", "Programmes", new { area = "", id = pid }, Request.Scheme)!);
            if (ok) Success(msg); else Error(msg);
            return RedirectToAction(nameof(Details), new { id = step.SessionId });
        }

        // POST: /Admin/Planner/Reject/12
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            var step = await _db.PlannerSteps.FindAsync(id);
            if (step == null) return NotFound();
            await _planner.RejectAsync(step, AdminName, reason);
            Success($"\"{step.Title}\" rejected.");
            return RedirectToAction(nameof(Details), new { id = step.SessionId });
        }

        // POST: /Admin/Planner/Publish/5  (session id)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var session = await _planner.GetSessionAsync(id);
            if (session == null) return NotFound();
            Success(await _planner.PublishAsync(session));
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/Planner/Cancel/5  (session id)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var session = await _planner.GetSessionAsync(id);
            if (session == null) return NotFound();
            await _planner.CancelAsync(session, AdminName);
            Success("Plan cancelled. Proposals still waiting were rejected.");
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
