using System.Net;
using Afas.Bvr.Crm;
using FakeItEasy;

namespace Afas.Bvr.Core.Tests;

[TestFixture()]
public class CrmDependenciesTests
{
  private HttpClient CreateHttpClient()
  {
    var _mockMessageHandler = A.Fake<HttpMessageHandler>();

    A.CallTo(_mockMessageHandler)
      .WithReturnType<Task<HttpResponseMessage>>()
      .Returns(new HttpResponseMessage
      {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent("{ \"name\": \"Leanne Graham\" }")
      });

    return new HttpClient(_mockMessageHandler);
  }

  [Test]
  public async Task ValidateExternalUserNameAsync_WithExistingUser_ThrowsException()
  {
    var depManager = new CrmDependencies(CreateHttpClient());
    Assert.ThrowsAsync<Exception>(() => depManager.ValidateExternalUserNameAsync("Leanne", "Graham"));
  }

  [Test]
  public async Task ValidateExternalUserNameAsync_WithNonExistingUser_DoesNotThrow()
  {
    var depManager = new CrmDependencies(CreateHttpClient());
    Assert.DoesNotThrowAsync(() => depManager.ValidateExternalUserNameAsync("Bart", "Vries"));
  }
}
