using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ChatChainCommon.DatabaseModels;
using ChatChainCommon.DatabaseServices;
using ChatChainServer.Models.MessageObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ChatChainServer.Hubs
{

    [Authorize]
    public class ChatChainHub:Hub
    {

        private readonly ILogger<ChatChainHub> _logger;
        private readonly GroupService _groupsContext;
        private readonly ClientService _clientsContext;

        private bool _hasSentLeaveMessage;

        public ChatChainHub(ILogger<ChatChainHub> logger, GroupService groupsContext, ClientService clientsContext)
        {
            _logger = logger;
            _groupsContext = groupsContext;
            _clientsContext = clientsContext;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation($"Connection: {Context.UserIdentifier}");
            foreach (Claim claim in Context.User.Claims)
            {
                _logger.LogInformation($"Claim: {claim.Properties}, {claim.Issuer}, {claim.Value}");
            }

            foreach (ClaimsIdentity identity in Context.User.Identities )
            {
                _logger.LogInformation($"identity: {identity.Name}");
            }
            _logger.LogInformation($"Claims: {Context.User.Claims}");
            
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (!_hasSentLeaveMessage)
            {
                ClientEventMessage message = new ClientEventMessage {Event = "STOP", SendToSelf = false};
                await SendClientEventMessage(message);
                _logger.LogInformation("Sent Client Event STOP message");
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendGenericMessage(GenericMessage message)
        {
            _logger.LogInformation($"Client {Context.UserIdentifier} had author: {message.User.Name} send \"{message.Message}\" in channel: {message.Group.GroupId}");

            Group group = await _groupsContext.GetFromGuidAsync(message.Group.GroupId);
            Client client = await _clientsContext.GetFromGuidAsync(Context.UserIdentifier);
            
            if (group != null && client != null && group.ClientIds.Contains(client.Id))
            {
                message.SendingClient = client;
                message.Group = group;
                foreach (Client fClient in await _groupsContext.GetClientsAsync(group.Id))
                {
                    if (!fClient.ClientId.Equals(client.ClientId) || message.SendToSelf)
                    {
                        await Clients.User(fClient.ClientGuid).SendAsync("ReceiveGenericMessage", message);
                    }
                }
            }
        }

        public async Task SendClientEventMessage(ClientEventMessage message)
        {
            _logger.LogInformation($"Client {Context.UserIdentifier} sent event: {message.Event} with extra data: {message.ExtraEventData}");

            Client client = await _clientsContext.GetFromGuidAsync(Context.UserIdentifier);

            if (client != null)
            {
                if (message.Event.Equals("STOP"))
                {
                    _hasSentLeaveMessage = true;
                }

                message.SendingClient = client;
                if (await _clientsContext.GetClientConfigAsync(client.Id) != null)
                {
                    foreach (ObjectId fGroupId in (await _clientsContext.GetClientConfigAsync(client.Id)).ClientEventGroups)
                    {
                        Group group = await _groupsContext.GetAsync(fGroupId);
                        
                        if (group == null) continue;
                        message.Group = group;
                        foreach (Client fClient in await _groupsContext.GetClientsAsync(group.Id))
                        {
                            if (!fClient.ClientId.Equals(client.ClientId) || message.SendToSelf)
                            {
                                await Clients.User(fClient.ClientGuid)
                                    .SendAsync("ReceiveClientEventMessage", message);
                            }
                        }
                    }
                }
            }
        }

        public async Task SendUserEventMessage(UserEventMessage message)
        {
            _logger.LogInformation($"Client {Context.UserIdentifier} with user: {message.User.Name} sent event: {message.Event} with extra data: {message.ExtraEventData}");

            Client client = await _clientsContext.GetFromGuidAsync(Context.UserIdentifier);

            if (client != null)
            {
                message.SendingClient = client;

                if (await _clientsContext.GetClientConfigAsync(client.Id) != null)
                {
                    foreach (ObjectId fGroupId in (await _clientsContext.GetClientConfigAsync(client.Id)).UserEventGroups)
                    {
                        Group group = await _groupsContext.GetAsync(fGroupId);
                        
                        if (group == null) continue;
                        message.Group = group;
                        foreach (Client fClient in await _groupsContext.GetClientsAsync(group.Id))
                        {
                            if (!fClient.ClientId.Equals(client.ClientId) || message.SendToSelf)
                            {
                                await Clients.User(fClient.ClientGuid)
                                    .SendAsync("ReceiveUserEventMessage", message);
                            }
                        }
                    }
                }
            }
        }
        
        public async Task GetGroups()
        {
            _logger.LogInformation($"Client {Context.UserIdentifier} requested their groups");
            
            GetGroupsResponse response = new GetGroupsResponse();

            Client client = await _clientsContext.GetFromGuidAsync(Context.UserIdentifier);

            if (client != null)
            {
                response.Groups = await _clientsContext.GetGroupsAsync(client.Id);
            }

            await Clients.Caller.SendAsync("ReceiveGroups", response);
        }
        
        public async Task GetClient()
        {
            _logger.LogInformation($"Client {Context.UserIdentifier} requested their Client");
            
            GetClientResponse response = new GetClientResponse();

            Client client = await _clientsContext.GetFromGuidAsync(Context.UserIdentifier);

            if (client != null)
            {
                response.Client = client;
            }

            await Clients.Caller.SendAsync("ReceiveClient", response);
        }
        
    }
}
