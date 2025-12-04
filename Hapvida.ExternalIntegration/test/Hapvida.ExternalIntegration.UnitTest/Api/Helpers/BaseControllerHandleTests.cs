using FluentAssertions;
using Hapvida.ExternalIntegration.Api.Helper;
using Hapvida.ExternalIntegration.Domain.Entities.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Api.Helpers;

public class BaseControllerHandleTests : BaseTest
{
    private readonly TestController _controller;

    public BaseControllerHandleTests()
    {
        _controller = new TestController();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public void HandleResponse_Should_Return_BadRequest_When_Response_Is_Null()
    {
        // Act
        var result = _controller.HandleResponse<object>(null!);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void HandleResponse_Should_Return_NotFound_When_StatusCode_Is_404()
    {
        // Arrange
        var response = new ApiResponse<object>
        {
            Success = false,
            StatusCode = 404,
            Message = "Not found"
        };

        // Act
        var result = _controller.HandleResponse(response);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(404);
        var problemDetails = objectResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Type.Should().Contain("not-found");
    }

    [Fact]
    public void HandleResponse_Should_Return_Conflict_When_StatusCode_Is_409()
    {
        // Arrange
        var response = new ApiResponse<object>
        {
            Success = false,
            StatusCode = 409,
            Message = "Conflict"
        };

        // Act
        var result = _controller.HandleResponse(response);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(409);
        var problemDetails = objectResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
    }

    [Fact]
    public void HandleResponse_Should_Return_BadRequest_When_Success_Is_False()
    {
        // Arrange
        var response = new ApiResponse<object>
        {
            Success = false,
            StatusCode = 400,
            Message = "Bad request"
        };

        // Act
        var result = _controller.HandleResponse(response);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(400);
        var problemDetails = objectResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
    }

    [Fact]
    public void HandleResponse_Should_Return_NoContent_When_Data_Is_Null()
    {
        // Arrange
        var response = new ApiResponse<object>
        {
            Success = true,
            StatusCode = 200,
            Data = null
        };

        // Act
        var result = _controller.HandleResponse(response);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void HandleResponse_Should_Return_NoContent_When_Collection_Is_Empty()
    {
        // Arrange
        var response = new ApiResponse<List<string>>
        {
            Success = true,
            StatusCode = 200,
            Data = new List<string>()
        };

        // Act
        var result = _controller.HandleResponse(response);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void HandleResponse_Should_Return_Ok_When_Collection_Has_Items()
    {
        // Arrange
        var response = new ApiResponse<List<string>>
        {
            Success = true,
            StatusCode = 200,
            Data = new List<string> { "item1", "item2" }
        };

        // Act
        var result = _controller.HandleResponse(response);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(206); // PartialContent
    }

    [Fact]
    public void HandleResponse_Should_Return_Created_When_StatusCode_Is_201()
    {
        // Arrange
        var response = new ApiResponse<string>
        {
            Success = true,
            StatusCode = 201,
            Data = "created"
        };

        // Act
        var result = _controller.HandleResponse(response);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
    }

    [Fact]
    public void HandleResponse_Should_Return_Ok_When_Success_Is_True()
    {
        // Arrange
        var response = new ApiResponse<string>
        {
            Success = true,
            StatusCode = 200,
            Data = "success"
        };

        // Act
        var result = _controller.HandleResponse(response);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    // Helper class para testar BaseControllerHandle
    private class TestController : BaseControllerHandle
    {
        public new ActionResult HandleResponse<TEntityResponse>(BaseResponse<TEntityResponse> response)
        {
            return base.HandleResponse(response);
        }
    }
}

