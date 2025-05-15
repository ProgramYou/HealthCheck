using HealthCheck.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using HealthCheck.Domain.Services;
using HealthCheck.Domain.Extensions;
using HealthCheck.Web.Services;

namespace HealthCheck.Web.Controllers
{
    public class HomeController(HealthService healthBase, LocalService localService,
            HealthSettings settings, IConfigurationManager configuration) : Controller
    {
        private const string standbyModeMessage = $"404. see /info";

        private string GetBaseUrl()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            return baseUrl;
        }

        [HttpGet]
        public IActionResult Index()
        {
            IActionResult result;

            try
            {
                var date = healthBase.GetDate();
                var baseUrl = GetBaseUrl();

                var nameOverride = healthBase.GetNameOverride();
                var hasNameOverride = !string.IsNullOrWhiteSpace(nameOverride);
                var servername = hasNameOverride ? nameOverride : Utility.GetServername();

                BaseTypes.ServerStatus stateOverride = healthBase.GetStateOverride();
                var state = healthBase.GetServerState(date, servername, stateOverride);

                if (state == BaseTypes.ServerStatus.Error || state == BaseTypes.ServerStatus.Invalid)
                    result = Content("E");
                else
                {
                    result = state == BaseTypes.ServerStatus.Active ? Content("OK") : NotFound(standbyModeMessage);
                }

            }
            catch (Exception)
            {
                result = Content("E");
            }

            return result;
        }

        [HttpGet]
        [Route("getname")]
        public string GetName()
        {
            return healthBase.GetEffectiveServername();
        }

        [Route("info")]
        public async Task<IActionResult> HealthCheck()
        {
            var date = healthBase.GetDate();
            var baseUrl = GetBaseUrl();
            var servername = healthBase.GetEffectiveServername();
            var serverLastChar = Utility.GetLastChar(servername);
            var hasIndex = int.TryParse(serverLastChar, out var serverNodeIndex);

            var loadBalancedUrl = healthBase.GetHealthCheckUrl();
            loadBalancedUrl = loadBalancedUrl == "" ? baseUrl : loadBalancedUrl;

            ViewBag.Date = date;
            ViewBag.Version = healthBase.Version;
            ViewBag.Server = servername;
            ViewBag.ServerLastChar = serverLastChar;
            ViewBag.ParityStatusWhenMatched = healthBase.GetParityStatusWhenMatched();
            ViewBag.Errors = healthBase.Errors;

            var startDays = healthBase.GetStartDays();
            ViewBag.StartDays = startDays;

            if (healthBase.GetInterval() == BaseTypes.Interval.DateRange)
                ViewBag.NrOfPeriods = startDays.Count;
            else
                ViewBag.NrOfPeriods = 12;

            ViewBag.HasIndex = hasIndex;

            PendingResult pending;

            if (hasIndex)
            {
                ViewBag.ServerNodeIndex = serverNodeIndex;
                var stateOverride = healthBase.GetStateOverride();
                ViewBag.HasStateOverride = stateOverride != BaseTypes.ServerStatus.Automatic && stateOverride != BaseTypes.ServerStatus.Invalid;
                var status = healthBase.GetServerState(date, servername, stateOverride);
                ViewBag.Status = status;
                pending = await healthBase.IsPending(servername, status, loadBalancedUrl);
            }
            else
            {
                pending = new PendingResult
                {
                    Value = false,
                    Message = $"Server '{servername}' is invalid. ",
                    HasError = true
                };
            }

            ViewBag.IsPending = pending.Value;
            ViewBag.HasError = pending.HasError;
            ViewBag.PendingMessage = pending.Message;
            ViewBag.LoadBalancedEndpoint = loadBalancedUrl;
            ViewBag.IndividualEndpoint = baseUrl;

            return View("Info");
        }

        [HttpGet]
        [Route("reload")]
        public IActionResult Reload()
        {
            settings = localService.MapSettings(configuration);
            return RedirectToAction("HealthCheck");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}