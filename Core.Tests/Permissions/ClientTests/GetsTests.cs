using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Core.DTO.GatewayResponses.Repositories.Client;
using Api.Core.DTO.GatewayResponses.Repositories.Organisation;
using Api.Core.DTO.UseCaseRequests.Client;
using Api.Core.DTO.UseCaseResponses.Client;
using Api.Core.Entities;
using Api.Core.Interfaces.Gateways.Repositories;
using Api.Core.UseCases.Client;
using Api.Tests.MockObjects;
using Moq;
using Xunit;

namespace Api.Tests.Permissions.ClientTests
{
    public class GetsTests
    {
        [Fact]
        public async Task GetClients_NonOrgUser_False()
        {
            // Arrange \\

            Guid orgId = Guid.NewGuid();

            OrganisationDetails mockOrganisation = new OrganisationDetails
            {
                Owner = Guid.NewGuid().ToString(),
                Id = orgId
            };

            // Mock Organisation Repo that returns mockOrganisation
            Mock<IOrganisationRepository> mockOrganisationRepository = new Mock<IOrganisationRepository>();
            mockOrganisationRepository
                .Setup(repo => repo.Get(It.IsAny<Guid>()))
                .ReturnsAsync(new GetOrganisationGatewayResponse(mockOrganisation, true));

            // Mock Client with ownerId set to OrgId
            Client mockClient = new Client
            {
                Id = Guid.NewGuid(),
                OwnerId = orgId
            };

            // Mock Client Repo that returns mockClient
            Mock<IClientRepository> mockClientRepository = new Mock<IClientRepository>();
            mockClientRepository
                .Setup(repo =>
                    repo.GetForOwner(It.IsAny<Guid>()))
                .ReturnsAsync(new GetClientsGatewayResponse(new List<Client> {mockClient}, true));

            // The Use Case we are testing (UpdateClient)
            GetClientsUseCase useCase =
                new GetClientsUseCase(mockOrganisationRepository.Object, mockClientRepository.Object);

            //3. The output port is the mechanism to pass response data from the use case to a Presenter
            //for final preparation to deliver to the UI/web page/api response etc.
            MockOutputPort<GetClientsResponse> mockOutputPort =
                new MockOutputPort<GetClientsResponse>();

            // Act \\

            //3. We need a request model to cary the data into the use case for the upper layer (UI, Controller, etc.)
            bool response = await useCase.HandleAsync(new GetClientsRequest(Guid.NewGuid().ToString(), Guid.NewGuid()),
                mockOutputPort);

            // Assert \\
            Assert.False(response);
            Assert.False(mockOutputPort.Response.Success);
            Assert.True(mockOutputPort.Response.CheckedPermissions);
            Assert.Contains("404", mockOutputPort.Response.Errors.Select(e => e.Code));
            Assert.Contains("Clients Not Found", mockOutputPort.Response.Errors.Select(e => e.Description));
        }

        [Fact]
        public async Task GetClients_OrgUser_True()
        {
            // Arrange \\

            Guid userId = Guid.NewGuid();

            Guid orgId = Guid.NewGuid();

            OrganisationDetails mockOrganisation = new OrganisationDetails
            {
                Owner = Guid.NewGuid().ToString(),
                Id = orgId,
                Users = new List<OrganisationUser>
                {
                    new OrganisationUser
                    {
                        Id = userId.ToString(),
                        Permissions = new List<OrganisationPermissions>()
                    }
                }
            };

            // Mock Organisation Repo that returns mockOrganisation
            Mock<IOrganisationRepository> mockOrganisationRepository = new Mock<IOrganisationRepository>();
            mockOrganisationRepository
                .Setup(repo => repo.Get(It.IsAny<Guid>()))
                .ReturnsAsync(new GetOrganisationGatewayResponse(mockOrganisation, true));

            // Mock Client with ownerId set to OrgId
            Client mockClient = new Client
            {
                Id = Guid.NewGuid(),
                OwnerId = orgId
            };

            // Mock Client Repo that returns mockClient
            Mock<IClientRepository> mockClientRepository = new Mock<IClientRepository>();
            mockClientRepository
                .Setup(repo =>
                    repo.GetForOwner(It.IsAny<Guid>()))
                .ReturnsAsync(new GetClientsGatewayResponse(new List<Client> {mockClient}, true));

            // The Use Case we are testing (UpdateClient)
            GetClientsUseCase useCase =
                new GetClientsUseCase(mockOrganisationRepository.Object, mockClientRepository.Object);

            //3. The output port is the mechanism to pass response data from the use case to a Presenter
            //for final preparation to deliver to the UI/web page/api response etc.
            MockOutputPort<GetClientsResponse> mockOutputPort =
                new MockOutputPort<GetClientsResponse>();

            // Act \\

            //3. We need a request model to cary the data into the use case for the upper layer (UI, Controller, etc.)
            bool response = await useCase.HandleAsync(new GetClientsRequest(userId.ToString(), Guid.NewGuid()),
                mockOutputPort);

            // Assert \\
            Assert.True(response);
            Assert.True(mockOutputPort.Response.Success);
            Assert.True(mockOutputPort.Response.CheckedPermissions);
            Assert.Empty(mockOutputPort.Response.Errors);
        }
    }
}