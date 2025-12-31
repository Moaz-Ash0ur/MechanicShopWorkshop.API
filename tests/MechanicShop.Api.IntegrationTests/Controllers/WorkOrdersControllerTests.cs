using Asp.Versioning;
using MechanicShop.Api.IntegrationTests.Common;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Model;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrders;
using MechanicShop.Contracts.Requests.WorkOrders;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Tests.Common.Security;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace MechanicShop.Api.IntegrationTests.Controllers
{
    [Collection(WebAppFactoryCollection.CollectionName)]
    public class WorkOrdersControllerTests(WebAppFactory webAppFactory)
    {
        private readonly AppHttpClient _client = webAppFactory.CreateAppHttpClient();
        private readonly IAppDbContext _context = webAppFactory.CreateAppDbContext();
        private readonly string pathVersion = "{pathVersion}";

        [Fact]
        public async Task GetWorkOrders_WithValidPagination_ShouldReturnPaginatedList()
        {

            //Inialize Http Client Request Wiht Auth
            var token = await _client.GenerateTokenAsync(TestUsers.Manager);

            _client.SetAuthorizationHeader(token);

            // send actual request
            var response = await _client.GetAsync($"{pathVersion}workorders?page=1&pageSize=10");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            //more check if you want
            var result = await response.Content.ReadFromJsonAsync<PaginatedList<WorkOrderListItemDto>>();
            Assert.NotNull(result);
            Assert.NotNull(result!.Items);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);

        }

        [Theory]
        [InlineData(0, 10, "Page must be greater than 0")]
        [InlineData(-1, 10, "Page must be greater than 0")]
        [InlineData(1, 0, "PageSize must be between 1 and 100")]
        [InlineData(1, 101, "PageSize must be between 1 and 100")]
        [InlineData(1, -1, "PageSize must be between 1 and 100")]
        public async Task GetWorkOrders_WithInValidPagination_ShouldReturnBadRequest(int page,int pageSize, string expectedError)
        {

            //Inialize Http Client Request Wiht Auth
            var token = await _client.GenerateTokenAsync(TestUsers.Manager);

            _client.SetAuthorizationHeader(token);

            // send actual request
            var response = await _client.GetAsync($"{pathVersion}workorders?page={page}&pageSize={pageSize}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            //more check if you want
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains(expectedError, content);

        }


        [Fact]
        public async Task GetWorkOrders_WithFilters_ShouldApplyFiltersCorrectly()
        {
            //Inialize Http Client Request Wiht Auth
            var token = await _client.GenerateTokenAsync(TestUsers.Manager);

            _client.SetAuthorizationHeader(token);

            var vehicleId = Guid.NewGuid();
            var laborId = Guid.NewGuid();
            const string searchTerm = "test";
            const int state = (int)WorkOrderState.InProgress;
            const int spot = (int)Contracts.Common.Spot.A;
            var startDateFrom = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
            var startDateTo = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var queryString = $"page=1&pageSize=10&searchTerm={searchTerm}&state={state}&vehicleId={vehicleId}&laborId={laborId}&spot={spot}&startDateFrom={startDateFrom}&startDateTo={startDateTo}";


            // send actual request
            var response = await _client.GetAsync($"{pathVersion}workorders?{queryString}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<PaginatedList<WorkOrderListItemDto>>();

            Assert.NotNull(result);

        }


        [Fact]
        public async Task GetWorkOrders_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var response = await _client.GetAsync($"{pathVersion}workorders?page=1&pageSize=10");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }



        [Fact]
        public async Task GetWorkOrderById_WihtValidId_ShouldReturnWorkOrder()
        {

            var token = await _client.GenerateTokenAsync(TestUsers.Manager);

            _client.SetAuthorizationHeader(token);

            var workOrder = WorkOrderTestDataBuilder.Create()
                  .ForToday()
                  .WithRepairTasks(await _context.RepairTasks.Take(1).ToListAsync())
                  .WithVehicle(_context.Vehicles.FirstOrDefault()!.Id)
                  .WithLabor(TestUsers.Labor01.Id)
                  .Build();

            _context.WorkOrders.Add(workOrder);

            await _context.SaveChangesAsync(default);

            try
            {
                var response = await _client.GetAsync($"{pathVersion}workorders/{workOrder.Id}");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var result = await response.Content.ReadFromJsonAsync<WorkOrderDto>();

                Assert.NotNull(result);
                Assert.Equal(workOrder.Id, result!.Id);
            }
            finally
            {
                await _context.WorkOrders
                    .Where(w => w.Id == workOrder.Id)
                    .ExecuteDeleteAsync();
            }



        }


        [Fact]
        public async Task GetWorkOrderById_WithInvalidId_ShouldReturnNotFound()
        {
            var token = await _client.GenerateTokenAsync(TestUsers.Manager);

            _client.SetAuthorizationHeader(token);

            var nonExistentId = Guid.NewGuid();

            var response = await _client.GetAsync($"{pathVersion}workorders/{nonExistentId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }


        [Fact]
        public async Task GetWorkOrderById_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            var workOrderId = Guid.NewGuid();

            var response = await _client.GetAsync($"{pathVersion}workorders/{workOrderId}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }



        [Fact]
        public async Task CreateWorkOrder_WithValidRequest_ShouldCreateWorkOrder()
        {
            var token = await _client.GenerateTokenAsync(TestUsers.Manager);

            _client.SetAuthorizationHeader(token);


            var laborId = Guid.Parse(TestUsers.Labor01.Id);
            var customer = (await _context.Customers.Include(c => c.Vehicles).FirstOrDefaultAsync())!;
            var vehicle = customer.Vehicles.FirstOrDefault()!;
            var repairTaskIds = _context.RepairTasks.Select(rt => rt.Id).Take(2).ToList()!;

            var request = new CreateWorkOrderRequest
            {
                Spot = Contracts.Common.Spot.B,
                VehicleId = vehicle.Id,
                StartAtUtc = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(12),
                LaborId = laborId,
                RepairTaskIds = repairTaskIds
            };

            WorkOrderDto? dto = null;
            try
            {
                var response = await _client.PostAsJsonAsync($"{pathVersion}workorders", request);

                dto = await response.Content.ReadFromJsonAsync<WorkOrderDto>();

                Assert.Equal(HttpStatusCode.Created, response.StatusCode);

                Assert.NotNull(dto);
            }
            finally
            {
                if (dto is not null)
                {
                    await _context.WorkOrders
                      .Where(w => w.Id == dto.Id)
                      .ExecuteDeleteAsync();
                }
            }


        }


        [Fact]
        public async Task CreateWorkOrder_WithInvalidRequest_ShouldReturnBadRequest()
        {
            var token = await _client.GenerateTokenAsync(TestUsers.Manager);

            _client.SetAuthorizationHeader(token);

            var request = new CreateWorkOrderRequest
            {
                VehicleId = Guid.Empty,
                StartAtUtc = default,
                RepairTaskIds = []
            };

            var response = await _client.PostAsJsonAsync($"{pathVersion}workorders", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }



        [Fact]
        public async Task CreateWorkOrder_WithoutManagerRole_ShouldReturnForbidden()
        {
            var token = await _client.GenerateTokenAsync(TestUsers.Labor02);

            _client.SetAuthorizationHeader(token);

            var request = new CreateWorkOrderRequest
            {
                Spot = Contracts.Common.Spot.A,
                VehicleId = Guid.NewGuid(),
                StartAtUtc = DateTime.UtcNow.AddHours(1),
                RepairTaskIds = [Guid.NewGuid()],
                LaborId = Guid.NewGuid()
            };

            var response = await _client.PostAsJsonAsync($"{pathVersion}workorders", request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }


        [Fact]
        public async Task RelocateWorkOrder_WithValidRequest_ShouldUpdateWorkOrder()
        {
            var token = await _client.GenerateTokenAsync(TestUsers.Manager);

            _client.SetAuthorizationHeader(token);

            var workOrder = WorkOrderTestDataBuilder.Create()
                   .ForToday()
                   .WithRepairTasks(await _context.RepairTasks.Take(1).ToListAsync())
                   .WithVehicle(_context.Vehicles.FirstOrDefault()!.Id)
                   .WithLabor(TestUsers.Labor01.Id)
                   .Build();

            _context.WorkOrders.Add(workOrder);

            await _context.SaveChangesAsync(default);

            var request = new RelocateWorkOrderRequest
            {
                NewStartAtUtc = DateTime.UtcNow.AddHours(2),
                NewSpot = Contracts.Common.Spot.B
            };

            try
            {
                var response = await _client.PutAsJsonAsync($"{pathVersion}workorders/{workOrder.Id}/relocation", request);

                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
            finally
            {
                await _context.WorkOrders
                     .Where(w => w.Id == workOrder.Id)
                     .ExecuteDeleteAsync();
            }
        }



        [Fact]
        public async Task RelocateWorkOrder_WithInvalidId_ShouldReturnNotFound()
        {
            var token = await _client.GenerateTokenAsync(TestUsers.Manager);

            _client.SetAuthorizationHeader(token);

            var nonExistentId = Guid.NewGuid();

            var request = new RelocateWorkOrderRequest
            {
                NewStartAtUtc = DateTime.UtcNow.AddHours(2),
                NewSpot = Contracts.Common.Spot.B
            };

            var response = await _client.PutAsJsonAsync($"{pathVersion}workorders/{nonExistentId}/relocation", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }



        [Fact]
        public async Task AssignLabor_WithValidRequest_ShouldAssignLabor()
        {
            var token = await _client.GenerateTokenAsync(TestUsers.Manager);

            _client.SetAuthorizationHeader(token);

            var workOrder = WorkOrderTestDataBuilder.Create()
                           .ForToday()
                           .WithRepairTasks(await _context.RepairTasks.Take(1).ToListAsync())
                           .WithVehicle(_context.Vehicles.FirstOrDefault()!.Id)
                           .WithLabor(TestUsers.Labor01.Id)
                           .Build();

            _context.WorkOrders.Add(workOrder);

            await _context.SaveChangesAsync(default);

            var request = new AssignLaborRequest
            {
                LaborId = TestUsers.Labor02.Id
            };

            try
            {
                var response = await _client.PutAsJsonAsync($"{pathVersion}workorders/{workOrder.Id}/labor", request);

                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
            finally
            {
                await _context.WorkOrders
                     .Where(w => w.Id == workOrder.Id)
                     .ExecuteDeleteAsync();
            }
        }



        [Fact]
        public async Task AssignLabor_WithInvalidLaborId_ShouldReturnBadRequest()
        {
            var token = await _client.GenerateTokenAsync(TestUsers.Manager);

            _client.SetAuthorizationHeader(token);

            var request = new AssignLaborRequest
            {
                LaborId = Guid.Empty.ToString()
            };

            var response = await _client.PutAsJsonAsync($"{pathVersion}workorders/{Guid.NewGuid()}/labor", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }







    }
}
