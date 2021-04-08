using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TimeTracking.Common.Wrapper;
using TimeTracking.ReportGenerator.Bl.Abstract;
using TimeTracking.ReportGenerator.Models;
using TimeTracking.ReportGenerator.Models.Requests;
using TimeTracking.ReportGenerator.Models.Responces;

namespace TimeTracking.ReportGenerator.Bl.Impl.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportExporter _reportExporter;
        private readonly ILogger<ReportService> _logger;
        private readonly IWorkLogClientService _workLogClientService;

        public ReportService(IReportExporter reportExporter,
            ILogger<ReportService> logger,
            IWorkLogClientService workLogClientService)
        {
            _reportExporter = reportExporter;
            _logger = logger;
            _workLogClientService = workLogClientService;
        }


        public async Task<ApiResponse<ReportExporterResponse>> GenerateReportAsync(
            ReportConfiguration reportConfiguration)
        {
            try
            {
                var reportGeneratorRequest = new ReportGeneratorRequest()
                {
                    ProjectId = reportConfiguration.ProjectId,
                    UserId = reportConfiguration.UserId,
                };
                var apiResponse = await _workLogClientService.GetUserActivities(reportGeneratorRequest);
                if (!apiResponse.IsSuccess)
                {
                    return apiResponse.ToFailed<ReportExporterResponse>();
                }

                var reportExportFile =
                    _reportExporter.GenerateReportForExport(apiResponse.Data, reportConfiguration.ReportFormatType);
                return new ApiResponse<ReportExporterResponse>(reportExportFile);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "");
                return ApiResponse<ReportExporterResponse>.InternalError();
            }
        }
    }
}