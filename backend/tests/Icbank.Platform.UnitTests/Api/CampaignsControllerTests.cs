using System.Reflection;
using FluentAssertions;
using Icbank.Platform.Api.Controllers;
using Icbank.Platform.Application.Campaigns;
using Icbank.Platform.Application.Campaigns.Queries;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Campaigns;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Api;

/// <summary>
/// Verifies <see cref="CampaignsController"/>: each of the two campaign books is gated by its own
/// RBAC page, and a detail identifier belonging to the other book reads as missing rather than
/// forbidden, so the route cannot be used to probe across the permission boundary.
/// </summary>
public sealed class CampaignsControllerTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly CampaignsController _controller;

    /// <summary>Initializes a new instance of the <see cref="CampaignsControllerTests"/> class.</summary>
    public CampaignsControllerTests()
    {
        _controller = new CampaignsController(_sender) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
    }

    [Theory]
    [InlineData(nameof(CampaignsController.GetInternalAsync), "internal_campaigns:view")]
    [InlineData(nameof(CampaignsController.GetInternalByIdAsync), "internal_campaigns:view")]
    [InlineData(nameof(CampaignsController.GetExternalAsync), "external_campaigns:view")]
    [InlineData(nameof(CampaignsController.GetExternalByIdAsync), "external_campaigns:view")]
    public void Endpoint_IsGatedByItsOwnPage(string methodName, string expectedPolicy)
    {
        MethodInfo method = typeof(CampaignsController).GetMethod(methodName)!;

        method.GetCustomAttribute<AuthorizeAttribute>()!.Policy.Should().Be(expectedPolicy);
    }

    [Fact]
    public async Task GetInternalByIdAsync_ExternalCampaign_ReadsAsMissingRatherThanForbidden()
    {
        ArrangeDetail(CampaignAudience.External);

        ActionResult<CampaignDto> result = await _controller.GetInternalByIdAsync(1, CancellationToken.None);

        StatusCode(result).Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetExternalByIdAsync_InternalCampaign_ReadsAsMissingRatherThanForbidden()
    {
        ArrangeDetail(CampaignAudience.Internal);

        ActionResult<CampaignDto> result = await _controller.GetExternalByIdAsync(1, CancellationToken.None);

        StatusCode(result).Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetInternalByIdAsync_HandlerReportedMissing_ReadsAs404()
    {
        _sender.Send(Arg.Any<GetCampaignByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<CampaignDto>.Failure(GetCampaignByIdQuery.CampaignNotFoundError));

        ActionResult<CampaignDto> result = await _controller.GetInternalByIdAsync(99, CancellationToken.None);

        StatusCode(result).Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetInternalByIdAsync_InternalCampaign_ReturnsIt()
    {
        ArrangeDetail(CampaignAudience.Internal);

        ActionResult<CampaignDto> result = await _controller.GetInternalByIdAsync(1, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<CampaignDto>()
            .Which.Audience.Should().Be("internal");
    }

    [Fact]
    public async Task GetExternalAsync_Always_AsksOnlyForTheExternalBook()
    {
        _sender.Send(Arg.Any<GetCampaignBoardQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<CampaignBoardDto>.Success(new CampaignBoardDto(
                new CampaignBoardKpisDto(0, 0, 0, 0, 0, 0, 0),
                Array.Empty<CampaignDto>(),
                new Dictionary<string, int>(StringComparer.Ordinal),
                Now)));

        await _controller.GetExternalAsync("running", CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<GetCampaignBoardQuery>(query => query.Audience == "external" && query.Status == "running"),
            Arg.Any<CancellationToken>());
    }

    private static int? StatusCode(ActionResult<CampaignDto> result)
        => (result.Result as ObjectResult)?.StatusCode;

    private void ArrangeDetail(CampaignAudience audience)
    {
        Campaign campaign = Application.Campaigns.GetCampaignBoardQueryHandlerTests.MakeCampaign(1, "C-01", audience);
        CampaignDto dto = CampaignMapper.ToDto(campaign, Array.Empty<CampaignDeliverable>(), Array.Empty<CampaignChannel>(), Now);
        _sender.Send(Arg.Any<GetCampaignByIdQuery>(), Arg.Any<CancellationToken>()).Returns(Result<CampaignDto>.Success(dto));
    }
}
