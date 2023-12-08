using DotNetCoreSqlDb.Data;
using DotNetCoreSqlDb.Hubs;
using DotNetCoreSqlDb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace DotNetCoreSqlDb.Controllers
{
    [ActionTimerFilter]
    public class ProfilesController : Controller
    {
        private readonly ILogger _logger;
        private readonly MyDatabaseContext _context;
        private readonly IDistributedCache _cache;
        private readonly IHubContext<NotificationsHub, INotificationsClient> _notificationsHub;
        private readonly string _ProfileItemsCacheKey = "ProfilesList";

        public ProfilesController(
            MyDatabaseContext context,
            IDistributedCache cache, 
            ILogger<TodosController> logger, 
            IHubContext<NotificationsHub, INotificationsClient> notificationsHub)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _notificationsHub = notificationsHub;
        }

        // GET: Profiles
        public async Task<IActionResult> Index()
        {
            var profiles = new List<Profile>();
            byte[]? ProfileListByteArray;

            ProfileListByteArray = await _cache.GetAsync(_ProfileItemsCacheKey);
            if (ProfileListByteArray != null && ProfileListByteArray.Length > 0)
            {
                profiles = ConvertData<Profile>.ByteArrayToObjectList(ProfileListByteArray);
            }
            else
            {
                profiles = await _context.Profile.ToListAsync();
                ProfileListByteArray = ConvertData<Profile>.ObjectListToByteArray(profiles);
                await _cache.SetAsync(_ProfileItemsCacheKey, ProfileListByteArray);
            }

            return View(profiles);
        }

        // GET: Profiles/AccountId/<accountId>
        [Route("Profiles/AccountId/{accountId}")]
        public async Task<IActionResult> AccountId(string? accountId)
        {
            byte[]? ProfileListByteArray;
            Profile? profile;

            if (accountId == null)
            {
                return NotFound();
            }

            ProfileListByteArray = await _cache.GetAsync(GetProfileItemCacheKey(accountId));

            if (ProfileListByteArray != null && ProfileListByteArray.Length > 0)
            {
                profile = ConvertData<Profile>.ByteArrayToObject(ProfileListByteArray);
            }
            else
            {
                profile = await _context.Profile.FirstOrDefaultAsync(m => m.AccountId == accountId);
                if (profile == null)
                {
                    if (Request.Headers.TryGetValue("AutoCreate", out var autoCreate) && autoCreate.ToString().ToLower() == "true")
                    {
                        if (ModelState.IsValid)
                        {
                            _logger.LogInformation("Creating new Profile for account: {}", accountId);
                            profile = new Profile
                            {
                                AccountId = accountId,
                                IdentityProvider = "Microsoft",
                                State = ProfileState.New,
                                CreatedDate = DateTime.UtcNow
                            };
                            _context.Add(profile);
                            await _context.SaveChangesAsync();
                            await _cache.RemoveAsync(_ProfileItemsCacheKey);
                            ProfileListByteArray = ConvertData<Profile>.ObjectToByteArray(profile);
                            await _cache.SetAsync(GetProfileItemCacheKey(accountId), ProfileListByteArray);

                            return Json(profile);
                        }
                        else
                        {
                            return StatusCode(500);
                        }
                    }
                    else
                    {
                        return NotFound();
                    }
                }   
            }

            if (Request.Headers.Accept.Contains("application/json"))
            {
                return Json(profile);
            }
            else
            {
                return View("Details", profile);
            }
        }

        // GET: Profiles/Validate/5
        public async Task<IActionResult> Validate(int id)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var profile = _context.Profile.SingleOrDefault(p => p.ID == id);
                    if (profile != null)
                    {
                        profile.State = ProfileState.Validated;
                        _context.Update(profile);
                        await _context.SaveChangesAsync();
                        await _cache.RemoveAsync(GetProfileItemCacheKey(profile.AccountId));
                        await _cache.RemoveAsync(_ProfileItemsCacheKey);

                        await _notificationsHub.Clients.All.AccountValidated(profile.AccountId);

                        if (Request.Headers.Accept.Contains("application/json"))
                        {
                            return Json(profile);
                        }
                        else
                        {
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    else
                    {
                        return Json(new Error
                        {
                            ErrorCode = "itemNotFound",
                            ErrorMessage = string.Format("Profile with id {0} not found", id),
                            RequestId = HttpContext.TraceIdentifier
                        });
                    }
                }
                catch (DbUpdateConcurrencyException e)
                {
                    return Json(new Error
                    {
                        ErrorCode = "dbError",
                        ErrorMessage = string.Format("Error updating DB: {0}", e.Message),
                        RequestId = HttpContext.TraceIdentifier
                    });
                }   
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Profiles/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var profile = _context.Profile.Find(id);
            if (profile != null)
            {
                _context.Profile.Remove(profile);
                await _context.SaveChangesAsync();
                await _cache.RemoveAsync(GetProfileItemCacheKey(profile.AccountId));
                await _cache.RemoveAsync(_ProfileItemsCacheKey);
                if (Request.Headers.Accept.Contains("application/json"))
                {
                    return Json("");
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                return Json(new Error
                {
                    ErrorCode = "itemNotFound",
                    ErrorMessage = string.Format("Profile with id {0} not found", id),
                    RequestId = HttpContext.TraceIdentifier
                });
            }
        }


        private bool ProfileExists(int id)
        {
            return _context.Profile.Any(e => e.ID == id);
        }

        private string GetProfileItemCacheKey(string? accountId)
        {
            return _ProfileItemsCacheKey + "_&_" + accountId;
        }
    }
}
