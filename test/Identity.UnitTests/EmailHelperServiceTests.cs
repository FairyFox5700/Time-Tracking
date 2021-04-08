using System.Threading.Tasks;
using AutoFixture;
using AutoMockHelper.Core;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using NUnit.Framework;
using TimeTracking.Common.Abstract;
using TimeTracking.Common.Email;
using TimeTracking.Common.Enums;
using TimeTracking.Common.Helpers;
using TimeTracking.Common.Wrapper;
using TimeTracking.Identity.BL.Impl.Services;
using TimeTracking.Identity.Entities;
using TimeTracking.Templates.Models;

namespace Identity.UnitTests
{
    [TestFixture]
    public class EmailHelperServiceTests : AutoMockContext<EmailHelperService>
    {
        private static Fixture Fixture = new Fixture();

        #region SendEmailConfirmationEmail

        [Test]
        public async Task SendEmailConfirmationEmail_WhenSendFailed_ReturnEmailSendFailedResponse()
        {
            var user = Fixture.Create<User>();
            var code = Fixture.Create<string>();
            var usrManager = MockHelpers.MockUserManager<User>();
            usrManager.Setup(e => e.GenerateEmailConfirmationTokenAsync(user))
                .ReturnsAsync(code);
            var mailModel = new MailModel()
            {
                ToEmail = user.Email,
                Subject = EmailPurpose.EmailConfirmation.ToString(),
            };
            var emailPurpose = EmailPurpose.EmailConfirmation;
            var viewModel = new ConfirmAccountEmailViewModel
            {
                ConfirmEmailUrl = "callbackUrl"
            };
            MockFor<IEmailService>().Setup(e => e.SendMessageWithPurpose(mailModel, emailPurpose, viewModel))
                .ReturnsAsync(false);

            var response = await ClassUnderTest.SendEmailConfirmationEmail(user);

            response.StatusCode.Should().Be(500);
            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.EmailSendFailed);
            response.ResponseException.ErrorMessage.Should().Be(ErrorCode.EmailSendFailed.GetDescription());
        }

        [Test]
        public async Task SendEmailConfirmationEmail_WhenSendSuccess_ReturnEmailSendFailedResponse()
        {
            var user = Fixture.Create<User>();
            var code = Fixture.Create<string>();
            MockFor<UserManager<User>>().Setup(e => e.GenerateEmailConfirmationTokenAsync(user))
                .ReturnsAsync(code);
            var mailModel = new MailModel()
            {
                ToEmail = user.Email,
                Subject = EmailPurpose.EmailConfirmation.ToString(),
            };
            var emailPurpose = EmailPurpose.EmailConfirmation;
            var viewModel = new ConfirmAccountEmailViewModel
            {
                ConfirmEmailUrl = "callbackUrl"
            };
            MockFor<IEmailService>().Setup(e => e.SendMessageWithPurpose(mailModel, emailPurpose, viewModel))
                .ReturnsAsync(true);

            var response = await ClassUnderTest.SendEmailConfirmationEmail(user);

            response.StatusCode.Should().Be(200);
            response.IsSuccess.Should().BeTrue();
        }


        #endregion
        /*   public async Task<ApiResponse> SendResetPasswordConfirmationEmail(User user)
        {
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = GetCallbackUrl(user.Id, code, _client.ResetPasswordPath);

            var emailSendingSuccess = await _emailService.SendMessageWithPurpose(new MailModel()
            {
                ToEmail = user.Email,
                Subject = EmailPurpose.ResetPassword.ToString(),
            },
                EmailPurpose.ResetPassword,
                new ResetPasswordViewModel()
                {
                    ResetPasswordUrl = callbackUrl,
                    UserName = user.UserName,
                });

            if (!emailSendingSuccess)
            {
                return new ApiResponse(
                    new ApiError()
                    {
                        ErrorCode = ErrorCode.EmailSendFailed,
                        ErrorMessage = ErrorCode.EmailSendFailed.GetDescription(),
                    });
            }
            return ApiResponse.Success();
        }
         public async Task<ApiResponse> SendEmailConfirmationEmail(User user)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = GetCallbackUrl(user.Id, code, _client.EmailConfirmationPath);

            var emailSendingSuccess = await _emailService.SendMessageWithPurpose(new MailModel()
            {
                ToEmail = user.Email,
                Subject = EmailPurpose.EmailConfirmation.ToString(),
            },
                EmailPurpose.EmailConfirmation,
                new ConfirmAccountEmailViewModel
                {
                    ConfirmEmailUrl = callbackUrl
                });

            if (!emailSendingSuccess)
            {
                return new ApiResponse(
                    new ApiError()
                    {
                        ErrorCode = ErrorCode.EmailSendFailed,
                        ErrorMessage = ErrorCode.EmailSendFailed.GetDescription(),
                    });
            }
            return ApiResponse.Success();
        }*/

    }
}