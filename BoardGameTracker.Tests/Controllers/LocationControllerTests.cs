using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels.Location;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Locations.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class LocationControllerTests
{
    private readonly Mock<ILocationService> _locationServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<LocationController>> _loggerMock;
    private readonly LocationController _controller;

    public LocationControllerTests()
    {
        _locationServiceMock = new Mock<ILocationService>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<LocationController>>();
            
        _controller = new LocationController(
            _locationServiceMock.Object, 
            _mapperMock.Object, 
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetLocations_ShouldReturnObjectResultWithMappedLocations_WhenServiceReturnsLocations()
    {
        var locations = new List<Location>
        {
            new() { Id = 1, Name = "Home" },
            new() { Id = 2, Name = "Office" }
        };

        var mappedLocations = new List<LocationViewModel>
        {
            new() { Id = "1", Name = "Home", PlayCount = 5 },
            new() { Id = "2", Name = "Office", PlayCount = 3 }
        };

        _locationServiceMock.Setup(x => x.GetLocations()).ReturnsAsync(locations);
        _mapperMock.Setup(x => x.Map<IList<LocationViewModel>>(locations)).Returns(mappedLocations);

        var result = await _controller.GetLocations();

        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.Value.Should().Be(mappedLocations);

        _locationServiceMock.Verify(x => x.GetLocations(), Times.Once);
        _mapperMock.Verify(x => x.Map<IList<LocationViewModel>>(locations), Times.Once);
        _locationServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLocations_ShouldReturnObjectResultWithEmptyList_WhenServiceReturnsEmptyList()
    {
        var locations = new List<Location>();
        var mappedLocations = new List<LocationViewModel>();

        _locationServiceMock.Setup(x => x.GetLocations()).ReturnsAsync(locations);
        _mapperMock.Setup(x => x.Map<IList<LocationViewModel>>(locations)).Returns(mappedLocations);

        var result = await _controller.GetLocations();

        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.Value.Should().Be(mappedLocations);

        _locationServiceMock.Verify(x => x.GetLocations(), Times.Once);
        _mapperMock.Verify(x => x.Map<IList<LocationViewModel>>(locations), Times.Once);
        _locationServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateLocation_ShouldReturnOkResultWithMappedLocation_WhenValidViewModelProvided()
    {
        var createViewModel = new CreateLocationViewModel { Name = "New Location" };
        var createdLocation = new Location { Id = 1, Name = "New Location" };
        var mappedResult = new LocationViewModel { Id = "1", Name = "New Location", PlayCount = 0 };

        _locationServiceMock.Setup(x => x.Create(It.IsAny<Location>())).ReturnsAsync(createdLocation);
        _mapperMock.Setup(x => x.Map<LocationViewModel>(createdLocation)).Returns(mappedResult);

        var result = await _controller.CreateLocation(createViewModel);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(mappedResult);

        _locationServiceMock.Verify(x => x.Create(It.Is<Location>(l => l.Name == createViewModel.Name)), Times.Once);
        _mapperMock.Verify(x => x.Map<LocationViewModel>(createdLocation), Times.Once);
        _locationServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateLocation_ShouldReturnBadRequest_WhenViewModelIsNull()
    {
        var result = await _controller.CreateLocation(null);

        result.Should().BeOfType<BadRequestResult>();

        _locationServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateLocation_ShouldReturnInternalServerError_WhenServiceThrowsException()
    {
        var createViewModel = new CreateLocationViewModel { Name = "Test Location" };
        var expectedException = new InvalidOperationException("Database error");

        _locationServiceMock.Setup(x => x.Create(It.IsAny<Location>())).ThrowsAsync(expectedException);

        var result = await _controller.CreateLocation(createViewModel);

        result.Should().BeOfType<StatusCodeResult>();
        var statusResult = result as StatusCodeResult;
        statusResult!.StatusCode.Should().Be(500);

        _locationServiceMock.Verify(x => x.Create(It.Is<Location>(l => l.Name == createViewModel.Name)), Times.Once);
        _locationServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedException.Message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateLocation_ShouldReturnOkResultWithViewModel_WhenValidViewModelProvided()
    {
        var viewModel = new LocationViewModel { Id = "1", Name = "Updated Location", PlayCount = 10 };
        var location = new Location { Id = 1, Name = "Updated Location" };

        _mapperMock.Setup(x => x.Map<Location>(viewModel)).Returns(location);
        _locationServiceMock.Setup(x => x.Update(location)).Returns(Task.CompletedTask);

        var result = await _controller.UpdateLocation(viewModel);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(viewModel);

        _mapperMock.Verify(x => x.Map<Location>(viewModel), Times.Once);
        _locationServiceMock.Verify(x => x.Update(location), Times.Once);
        _mapperMock.VerifyNoOtherCalls();
        _locationServiceMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateLocation_ShouldReturnBadRequest_WhenViewModelIsNull()
    {
        var result = await _controller.UpdateLocation(null);

        result.Should().BeOfType<BadRequestResult>();

        _locationServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateLocation_ShouldReturnInternalServerError_WhenServiceThrowsException()
    {
        var viewModel = new LocationViewModel { Id = "1", Name = "Test Location", PlayCount = 5 };
        var location = new Location { Id = 1, Name = "Test Location" };
        var expectedException = new InvalidOperationException("Update failed");

        _mapperMock.Setup(x => x.Map<Location>(viewModel)).Returns(location);
        _locationServiceMock.Setup(x => x.Update(location)).ThrowsAsync(expectedException);

        var result = await _controller.UpdateLocation(viewModel);

        result.Should().BeOfType<StatusCodeResult>();
        var statusResult = result as StatusCodeResult;
        statusResult!.StatusCode.Should().Be(500);

        _mapperMock.Verify(x => x.Map<Location>(viewModel), Times.Once);
        _locationServiceMock.Verify(x => x.Update(location), Times.Once);
        _mapperMock.VerifyNoOtherCalls();
        _locationServiceMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(999)]
    [InlineData(int.MaxValue)]
    public async Task DeleteLocation_ShouldHandleDifferentIds_WhenValidIdsProvided(int locationId)
    {
        _locationServiceMock.Setup(x => x.Delete(locationId)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteLocation(locationId);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var deletionResult = okResult!.Value as DeletionResultViewModel;
        deletionResult!.Type.Should().Be((int)ResultState.Success);

        _locationServiceMock.Verify(x => x.Delete(locationId), Times.Once);
        _locationServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Home")]
    [InlineData("Very Long Location Name With Spaces And Special Characters!@#")]
    public async Task CreateLocation_ShouldHandleDifferentNames_WhenValidNamesProvided(string locationName)
    {
        var createViewModel = new CreateLocationViewModel { Name = locationName };
        var createdLocation = new Location { Id = 1, Name = locationName };
        var mappedResult = new LocationViewModel { Id = "1", Name = locationName, PlayCount = 0 };

        _locationServiceMock.Setup(x => x.Create(It.IsAny<Location>())).ReturnsAsync(createdLocation);
        _mapperMock.Setup(x => x.Map<LocationViewModel>(createdLocation)).Returns(mappedResult);

        var result = await _controller.CreateLocation(createViewModel);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(mappedResult);

        _locationServiceMock.Verify(x => x.Create(It.Is<Location>(l => l.Name == locationName)), Times.Once);
        _mapperMock.Verify(x => x.Map<LocationViewModel>(createdLocation), Times.Once);
        _locationServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateLocation_ShouldCreateLocationWithCorrectProperties_WhenValidViewModelProvided()
    {
        var createViewModel = new CreateLocationViewModel { Name = "Test Location" };
        var createdLocation = new Location { Id = 123, Name = "Test Location" };
        var mappedResult = new LocationViewModel { Id = "123", Name = "Test Location", PlayCount = 0 };

        _locationServiceMock.Setup(x => x.Create(It.IsAny<Location>())).ReturnsAsync(createdLocation);
        _mapperMock.Setup(x => x.Map<LocationViewModel>(createdLocation)).Returns(mappedResult);

        var result = await _controller.CreateLocation(createViewModel);

        result.Should().BeOfType<OkObjectResult>();

        _locationServiceMock.Verify(x => x.Create(It.Is<Location>(l => 
            l.Name == createViewModel.Name && 
            l.Id == 0)), Times.Once);
        _mapperMock.Verify(x => x.Map<LocationViewModel>(createdLocation), Times.Once);
        _locationServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }
}