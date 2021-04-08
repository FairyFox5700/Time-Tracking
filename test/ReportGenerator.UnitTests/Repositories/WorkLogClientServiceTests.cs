using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoMockHelper.Core;
using Castle.Core.Logging;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using TimeTracking.Common.HttpClientHandler;
using TimeTracking.Common.Wrapper;
using TimeTracking.ReportGenerator.Bl.Impl.Services;
using TimeTracking.ReportGenerator.Bl.Impl.Settings;
using TimeTracking.ReportGenerator.Models;
using TimeTracking.ReportGenerator.Models.Requests;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace ReportGenerator.UnitTests.Repositories
{
    [TestFixture]
    public class WorkLogClientServiceTests : AutoMockContext<WorkLogClientService>
    {
        private static Fixture Fixture = new Fixture();

        [Test]
        public async Task GetUserActivities_WhenSendSuccessfully_ReturnListOfUserActivities_()
        {
            var request = Fixture.Freeze<ReportGeneratorRequest>();
            var expectedActivities = Fixture.Create<ApiResponse<UserActivityDto>>();
            MockHttpContextGetToken(MockFor<IHttpContextAccessor>(), "access_token", "qwqw");
            MockFor<IOptions<TimeTrackingClientSettings>>().Setup(e => e.Value)
                .Returns(new TimeTrackingClientSettings()
                {
                    Url = "http://url/",
                    IdentityUrl = "http://identity.url/"
                });
            HttpResponseMessage httpMessage = new HttpResponseMessage();
            httpMessage.StatusCode = System.Net.HttpStatusCode.OK;//Setting statuscode    
            httpMessage.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(expectedActivities));
            httpMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            MockFor<IHttpProvider>().Setup(e => e.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(httpMessage)
                .Verifiable();

            var response = await ClassUnderTest.GetUserActivities(request);

            response.Data.Should().BeEquivalentTo(expectedActivities.Data);
        }


        [Test]
        public async Task GetUserActivities_WhenExceptionThrown_ShouldReturnInternalError()
        {
            var request = Fixture.Freeze<ReportGeneratorRequest>();
            var expectedActivities = Fixture.Create<ApiResponse<UserActivityDto>>();
            MockHttpContextGetToken(MockFor<IHttpContextAccessor>(), "access_token", "qwqw");
            MockFor<IOptions<TimeTrackingClientSettings>>().Setup(e => e.Value)
                .Returns(new TimeTrackingClientSettings()
                {
                    Url = "http://url/",
                    IdentityUrl = "http://identity.url/"
                });
            HttpResponseMessage responseh = new HttpResponseMessage();
            responseh.StatusCode = System.Net.HttpStatusCode.OK;//Setting statuscode    
            responseh.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(expectedActivities));
            responseh.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            MockFor<IHttpProvider>().Setup(e => e.GetAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception());

            var response = await ClassUnderTest.GetUserActivities(request);

            response.VerifyInternalError();
        }



        private void MockHttpContextGetToken(
            Mock<IHttpContextAccessor> httpContextAccessorMock,
            string tokenName, string tokenValue, string scheme = null)
        {
            var authenticationServiceMock = new Mock<IAuthenticationService>();
            httpContextAccessorMock
                .Setup(x => x.HttpContext.RequestServices.GetService(typeof(IAuthenticationService)))
                .Returns(authenticationServiceMock.Object);

            var authResult = AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(), scheme));

            authResult.Properties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = tokenName, Value = tokenValue }
            });

            authenticationServiceMock
                .Setup(x => x.AuthenticateAsync(httpContextAccessorMock.Object.HttpContext, scheme))
                .ReturnsAsync(authResult);
        }

        [Test]
        public async Task GetUserActivities_WhenConstructorWithNullSetting_ShouldThrowArgumentBullExceprption()
        {
            Func<WorkLogClientService> act = () => new WorkLogClientService(
                 MockFor<IHttpProvider>().Object,
                 null,
                 MockFor<IHttpContextAccessor>().Object,
                 MockFor<ILogger<WorkLogClientService>>().Object);

            act.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null. (Parameter 'settings')");
        }

    }
}