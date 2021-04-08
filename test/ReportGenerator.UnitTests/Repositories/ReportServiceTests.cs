using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoMockHelper.Core;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using TimeTracking.Common.Wrapper;
using TimeTracking.ReportGenerator.Bl.Abstract;
using TimeTracking.ReportGenerator.Bl.Impl.Services;
using TimeTracking.ReportGenerator.Models;
using TimeTracking.ReportGenerator.Models.Requests;
using TimeTracking.ReportGenerator.Models.Responces;

namespace ReportGenerator.UnitTests.Repositories
{
    [TestFixture]
    public class ReportServiceTests : AutoMockContext<ReportService>
    {

        private static Fixture Fixture = new Fixture();

        [Test]
        public async Task GenerateReportAsync_WhenHttpClientFailedResponseReturned_ShoulReturnHttpClientFailedResponse()
        {
            var parameters = Fixture.Freeze<ReportConfiguration>();
            var workLogResponse = new ApiResponse<UserActivityDto>() { IsSuccess = false };
            MockFor<IWorkLogClientService>().Setup(e => e.GetUserActivities(It.IsAny<ReportGeneratorRequest>()))
                .ReturnsAsync(workLogResponse);

            var response = await ClassUnderTest.GenerateReportAsync(parameters);

            response.Should().BeEquivalentTo(workLogResponse.ToFailed<ReportExporterResponse>());

        }

        [Test]
        public async Task GenerateReportAsync_WhenExceptionThrown_ShouldReturnInternalError()
        {
            var parameters = Fixture.Freeze<ReportConfiguration>();
            var workLogResponse = new ApiResponse<UserActivityDto>() { IsSuccess = true, Data = Fixture.Create<UserActivityDto>() };
            MockFor<IWorkLogClientService>().Setup(e => e.GetUserActivities(It.IsAny<ReportGeneratorRequest>()))
                .ReturnsAsync(workLogResponse);
            MockFor<IReportExporter>()
                .Setup(e => e.GenerateReportForExport(workLogResponse.Data, parameters.ReportFormatType))
                .Throws(new Exception());

            var response = await ClassUnderTest.GenerateReportAsync(parameters);

            response.VerifyInternalError();
        }

        [Test]
        public async Task GenerateReportAsync_WhenResponseGenerate_ShouldReturnGeneratedResponse()
        {
            var parameters = Fixture.Freeze<ReportConfiguration>();
            var reportGeneratedResponse = Fixture.Create<ReportExporterResponse>();
            var workLogResponse = new ApiResponse<UserActivityDto>() { IsSuccess = true, Data = Fixture.Create<UserActivityDto>() };
            MockFor<IWorkLogClientService>().Setup(e => e.GetUserActivities(It.IsAny<ReportGeneratorRequest>()))
                .ReturnsAsync(workLogResponse);
            MockFor<IReportExporter>()
                .Setup(e => e.GenerateReportForExport(workLogResponse.Data, parameters.ReportFormatType))
                .Returns(reportGeneratedResponse);

            var response = await ClassUnderTest.GenerateReportAsync(parameters);

            response.VerifySuccessResponseWithData(reportGeneratedResponse);

        }
    }
}