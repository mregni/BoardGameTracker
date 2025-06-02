using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Locations;
using BoardGameTracker.Core.Locations.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class LocationServiceTests
{
    private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly LocationService _locationService;

        public LocationServiceTests()
        {
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _locationService = new LocationService(_locationRepositoryMock.Object);
        }

        [Fact]
        public async Task GetLocations_ShouldReturnLocationList_WhenRepositoryReturnsData()
        {
            var expectedLocations = new List<Location>
            {
                new() { Id = 1, Name = "Home" },
                new() { Id = 2, Name = "Office" },
                new() { Id = 3, Name = "Park" }
            };

            _locationRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedLocations);

            var result = await _locationService.GetLocations();

            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().BeEquivalentTo(expectedLocations);
            _locationRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetLocations_ShouldReturnEmptyList_WhenRepositoryReturnsEmptyList()
        {
            var expectedLocations = new List<Location>();

            _locationRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedLocations);

            var result = await _locationService.GetLocations();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
            _locationRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetLocations_ShouldThrowException_WhenRepositoryThrows()
        {
            var expectedException = new InvalidOperationException("Repository error");

            _locationRepositoryMock.Setup(x => x.GetAllAsync()).ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _locationService.GetLocations());

            exception.Should().Be(expectedException);
            _locationRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_ShouldReturnCreatedLocation_WhenRepositorySucceeds()
        {
            var inputLocation = new Location { Name = "New Location" };
            var expectedLocation = new Location { Id = 1, Name = "New Location" };

            _locationRepositoryMock.Setup(x => x.CreateAsync(inputLocation)).ReturnsAsync(expectedLocation);

            var result = await _locationService.Create(inputLocation);

            result.Should().NotBeNull();
            result.Should().Be(expectedLocation);
            _locationRepositoryMock.Verify(x => x.CreateAsync(inputLocation), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_ShouldPassCorrectLocation_WhenCalled()
        {
            var location = new Location { Id = 5, Name = "Test Location" };
            var expectedLocation = new Location { Id = 5, Name = "Test Location" };

            _locationRepositoryMock.Setup(x => x.CreateAsync(location)).ReturnsAsync(expectedLocation);

            var result = await _locationService.Create(location);

            result.Should().Be(expectedLocation);
            _locationRepositoryMock.Verify(x => x.CreateAsync(
                It.Is<Location>(l => l.Id == location.Id && l.Name == location.Name)), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_ShouldThrowException_WhenRepositoryThrows()
        {
            var location = new Location { Name = "Test Location" };
            var expectedException = new ArgumentException("Invalid location");

            _locationRepositoryMock.Setup(x => x.CreateAsync(location)).ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _locationService.Create(location));

            exception.Should().Be(expectedException);
            _locationRepositoryMock.Verify(x => x.CreateAsync(location), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Delete_ShouldCallRepositoryDelete_WhenCalled()
        {
            const int locationId = 1;

            _locationRepositoryMock.Setup(x => x.DeleteAsync(locationId)).ReturnsAsync(true);

            await _locationService.Delete(locationId);

            _locationRepositoryMock.Verify(x => x.DeleteAsync(locationId), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(999)]
        [InlineData(int.MaxValue)]
        public async Task Delete_ShouldPassCorrectId_WhenCalledWithDifferentIds(int id)
        {
            _locationRepositoryMock.Setup(x => x.DeleteAsync(id)).ReturnsAsync(true);

            await _locationService.Delete(id);

            _locationRepositoryMock.Verify(x => x.DeleteAsync(id), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Delete_ShouldThrowException_WhenRepositoryThrows()
        {
            const int locationId = 1;
            var expectedException = new KeyNotFoundException("Location not found");

            _locationRepositoryMock.Setup(x => x.DeleteAsync(locationId)).ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _locationService.Delete(locationId));

            exception.Should().Be(expectedException);
            _locationRepositoryMock.Verify(x => x.DeleteAsync(locationId), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldCallRepositoryUpdate_WhenCalled()
        {
            var location = new Location { Id = 1, Name = "Updated Location" };

            _locationRepositoryMock.Setup(x => x.UpdateAsync(location)).ReturnsAsync(location);

            await _locationService.Update(location);

            _locationRepositoryMock.Verify(x => x.UpdateAsync(location), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldPassCorrectLocation_WhenCalled()
        {
            var location = new Location { Id = 5, Name = "Updated Name" };

            _locationRepositoryMock.Setup(x => x.UpdateAsync(location)).ReturnsAsync(location);

            await _locationService.Update(location);

            _locationRepositoryMock.Verify(x => x.UpdateAsync(
                It.Is<Location>(l => l.Id == location.Id && l.Name == location.Name)), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldThrowException_WhenRepositoryThrows()
        {
            var location = new Location { Id = 1, Name = "Test Location" };
            var expectedException = new InvalidOperationException("Update failed");

            _locationRepositoryMock.Setup(x => x.UpdateAsync(location)).ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _locationService.Update(location));

            exception.Should().Be(expectedException);
            _locationRepositoryMock.Verify(x => x.UpdateAsync(location), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CountAsync_ShouldReturnCount_WhenRepositoryReturnsCount()
        {
            const int expectedCount = 42;

            _locationRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(expectedCount);

            var result = await _locationService.CountAsync();

            result.Should().Be(expectedCount);
            _locationRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public async Task CountAsync_ShouldReturnCorrectCount_WithDifferentCounts(int count)
        {
            _locationRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(count);

            var result = await _locationService.CountAsync();

            result.Should().Be(count);
            _locationRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CountAsync_ShouldThrowException_WhenRepositoryThrows()
        {
            var expectedException = new TimeoutException("Database timeout");

            _locationRepositoryMock.Setup(x => x.CountAsync()).ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => _locationService.CountAsync());

            exception.Should().Be(expectedException);
            _locationRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
            _locationRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task LocationService_ShouldPassThroughAllRepositoryResults_WhenCalled()
        {
            var locations = new List<Location> { new() { Id = 1, Name = "Test" } };
            var createdLocation = new Location { Id = 2, Name = "Created" };
            const int count = 5;

            _locationRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(locations);
            _locationRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Location>())).ReturnsAsync(createdLocation);
            _locationRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(count);

            var getAllResult = await _locationService.GetLocations();
            var createResult = await _locationService.Create(new Location());
            var countResult = await _locationService.CountAsync();

            getAllResult.Should().BeSameAs(locations);
            createResult.Should().BeSameAs(createdLocation);
            countResult.Should().Be(count);
        }
}