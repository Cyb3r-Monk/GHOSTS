﻿// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Threading;
using System.Threading.Tasks;
using Ghosts.Api.Models;
using Ghosts.Api.Services;
using Ghosts.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using NLog;

namespace Ghosts.Api.Controllers
{
    /// <summary>
    /// GHOSTS CLIENT CONTROLLER
    /// These endpoints are typically only used by GHOSTS Clients installed and configured to use the GHOSTS C2
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ClientUpdatesController : Controller
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IBackgroundQueue _queue;
        private readonly IMachineUpdateService _updateService;
        private readonly IMachineService _machineService;

        public ClientUpdatesController(IMachineService machineService, IMachineUpdateService updateService, IBackgroundQueue queue)
        {
            _updateService = updateService;
            _queue = queue;
            _machineService = machineService;
        }

        /// <summary>
        /// Clients use this endpoint to check for updates for them to download
        /// </summary>
        /// <param name="ct">Cancellation Token</param>
        /// <returns>404 if no update, or a json payload of a particular update</returns>
        /// <response code="200">Returns json payload of a particular update</response>
        /// <response code="401">Unauthorized or incorrectly formatted machine request</response>
        /// <response code="404">No Update</response>
        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var id = Request.Headers["ghosts-id"];
            log.Trace($"Request by {id}");
            
            var findMachineResponse = await this._machineService.FindOrCreate(HttpContext, ct);
            if (!findMachineResponse.IsValid())
            {
                return StatusCode(StatusCodes.Status401Unauthorized, findMachineResponse.Error);
            }

            var m = findMachineResponse.Machine;

            _queue.Enqueue(
                new QueueEntry
                {
                    Payload =
                        new MachineQueueEntry
                        {
                            Machine = m,
                            LogDump = null,
                            HistoryType = Machine.MachineHistoryItem.HistoryType.RequestedUpdates
                        },
                    Type = QueueEntry.Types.Machine
                });

            //check dB for new updates to deliver
            var u = await _updateService.GetAsync(m.Id, m.CurrentUsername, ct);
            if (u == null) return NotFound();

            log.Trace($"Update sent to {m.Id} {m.FQDN} {u.Id} {u.Username} {u.Update}");
            
            var update = new UpdateClientConfig { Type = u.Type, Update = u.Update };

            await _updateService.DeleteAsync(u.Id, m.Id, ct);

            // integrators want to know that a timeline was actually delivered
            // (the service only guarantees that the update was received)
            _queue.Enqueue(
                new QueueEntry
                {
                    Payload =
                        new NotificationQueueEntry()
                        {
                            Type = NotificationQueueEntry.NotificationType.TimelineDelivered,
                            Payload = (JObject)JToken.FromObject(update)
                        },
                    Type = QueueEntry.Types.Notification
                });

            return Json(update);
        }
    }
}