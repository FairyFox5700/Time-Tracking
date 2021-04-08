using System.Collections.Generic;
using System.ComponentModel;
using FluentValidation.Results;

namespace TimeTracking.Common.Wrapper
{
    public class ApiError
    {
        public ErrorCode ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public IEnumerable<ValidationFailure> ValidationErrors { get; set; }

        public ApiError()
        {

        }
        public ApiError(ErrorCode errorCode, string errorMessage)
        {
            this.ErrorCode = errorCode;
            this.ErrorMessage = errorMessage;
        }

        public ApiError(ErrorCode errorCode, IEnumerable<ValidationFailure> validationErrors)
        {
            this.ErrorCode = errorCode;
            this.ValidationErrors = validationErrors;
        }
    }

    public enum ErrorCode
    {
        [Description("Could not find user")]
        UserNotFound,
        [Description("Provided password is invalid")]
        InvalidCurrentPassword,
        [Description("Can not found role")]
        RoleNotFound,
        [Description("User registration failed")]
        UserRegistrationFailed,
        [Description("Assigning user to role failed")]
        AddUserToRoleFailed,
        [Description("Email confirmation failed")]
        EmailConfirmationFailed,
        [Description("Reset password failed")]
        ResetPasswordFailed,
        [Description("Validation errors")]
        ValidationError,
        [Description("Issue creation failed")]
        IssueCreationFailed,
        [Description("Issue not found")]
        IssueNotFound,
        [Description("Internal error")]
        InternalError,
        [Description("Milestone not found")]
        MileStoneNotFound,
        [Description("Project not found")]
        ProjectNotFound,
        [Description("Milestone creation failed")]
        MilestoneCreationFiled,
        [Description("Team not found")]
        TeamNotFound,
        [Description("WorkLog creation failed")]
        WorkLogCreationFailed,
        [Description("WorkLog not found")]
        WorkLogNotFound,
        [Description("Refresh token revokation failed")]
        RefreshTokenRevocationFailed,
        [Description("Failed to send email")]
        EmailSendFailed,
        [Description("An error caused by client")]
        ClientError,
        [Description("An user to role failed")]
        AddToRoleFailed,
        [Description("Project creation  failed")]
        ProjectCreationFailed,
        [Description("Team creation  failed")]
        TeamCreationFailed,
    }
}