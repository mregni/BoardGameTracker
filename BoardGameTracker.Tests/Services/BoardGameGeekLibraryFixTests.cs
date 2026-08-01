using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Xunit;

namespace BoardGameTracker.Tests.Services;

/// <summary>
/// Guards the patched BoardGamer.BoardGameGeek (0.10.0): a collection whose &lt;items&gt;
/// element has no/invalid pubdate must no longer throw ArgumentOutOfRangeException when the host
/// runs in a timezone east of UTC. The unpatched 0.10.0 assigns AttributeValueAsDateTime("pubdate")
/// (DateTime.MinValue) into the DateTimeOffset PublishDate property, which throws.
/// </summary>
public class BoardGameGeekLibraryFixTests
{
    private static BoardGameGeekXmlApi2Client CreateClient(string xml)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(xml, Encoding.UTF8, "text/xml")
            });

        var httpClient = new HttpClient(handler.Object);
        return new BoardGameGeekXmlApi2Client(httpClient, new BoardGameGeekXmlApi2ClientOptions());
    }

    private static string CollectionXml(string pubDateAttribute) =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        $"<items totalitems=\"1\" termsofuse=\"https://boardgamegeek.com/xmlapi/termsofuse\"{pubDateAttribute}>" +
        "<item objecttype=\"thing\" objectid=\"13\" subtype=\"boardgame\" collid=\"1\">" +
        "<name sortindex=\"1\">Catan</name><yearpublished>1995</yearpublished>" +
        "<image>https://example.com/catan.jpg</image>" +
        "<status own=\"1\" prevowned=\"0\" fortrade=\"0\" want=\"0\" lastmodified=\"2024-01-15 10:30:00\" />" +
        "<numplays>3</numplays></item></items>";

    [Fact]
    public async Task GetCollectionAsync_ShouldNotThrow_AndDefaultPublishDate_WhenPubdateMissing()
    {
        // The reproduction of the reported bug: no pubdate attribute at all.
        var client = CreateClient(CollectionXml(pubDateAttribute: string.Empty));

        var response = await client.GetCollectionAsync(new CollectionRequest("someuser"));

        response.Succeeded.Should().BeTrue();
        response.Result.PublishDate.Should().Be(default(DateTimeOffset));
        response.Result.Count.Should().Be(1);
        var item = response.Result.First();
        item.ObjectId.Should().Be(13);
        item.Name.Should().Be("Catan");
        item.SubType.Should().Be("boardgame");
    }

    [Fact]
    public async Task GetCollectionAsync_ShouldParsePublishDateWithOffset_WhenPubdatePresent()
    {
        var client = CreateClient(CollectionXml(pubDateAttribute: " pubdate=\"Fri, 20 Sep 2024 05:00:00 +0000\""));

        var response = await client.GetCollectionAsync(new CollectionRequest("someuser"));

        response.Succeeded.Should().BeTrue();
        response.Result.PublishDate.Should().Be(new DateTimeOffset(2024, 9, 20, 5, 0, 0, TimeSpan.Zero));
    }
}
