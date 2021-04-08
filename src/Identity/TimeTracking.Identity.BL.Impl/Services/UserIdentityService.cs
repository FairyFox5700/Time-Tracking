using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Initializers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TimeTracking.Common.Helpers;
using TimeTracking.Common.Mappers;
using TimeTracking.Common.Wrapper;
using TimeTracking.Contracts.Events;
using TimeTracking.Identity.BL.Abstract.Services;
using TimeTracking.Identity.Dal.Abstract;
using TimeTracking.Identity.Dal.Impl.Seed.Data;
using TimeTracking.Identity.Entities;
using TimeTracking.Identity.Models.Dtos;
using TimeTracking.Identity.Models.Requests;

namespace TimeTracking.Identity.BL.Impl.Services
{
    public class UserIdentityService : IUserIdentityService
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly ISystemClock _systemClock;
        private readonly IEmailHelperService _emailHelperService;
        private readonly IBaseMapper<User, UserDto> _userMapper;
        private readonly IPublishEndpoint _publish;
        private readonly ILogger<UserIdentityService> _logger;

        public UserIdentityService(UserManager<User> userManager,
            IUserRepository userRepository,
            ISystemClock systemClock,
            IEmailHelperService emailHelperService,
            IBaseMapper<User, UserDto> userMapper,
            ILogger<UserIdentityService> logger,
            IPublishEndpoint publish)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _systemClock = systemClock;
            _emailHelperService = emailHelperService;
            _userMapper = userMapper;
            _logger = logger;
            _publish = publish;
        }

        public async Task<ApiResponse> RegisterAsync(RegistrationRequest model)
        {
            var userToAdd = ConstructUserModelToAdd(model);
            var result = await _userManager.CreateAsync(userToAdd, model.Password);
            if (!result.Succeeded)
            {
                _logger.LogWarning(result.ToString());
                return new ApiResponse(
                    new ApiError()
                    {
                        ErrorCode = ErrorCode.UserRegistrationFailed,
                        ErrorMessage = result.ToString(),
                    });
            }

            var sendEmailConfirmResult = await _emailHelperService.SendEmailConfirmationEmail(userToAdd);
            if (!sendEmailConfirmResult.IsSuccess)
            {
                return sendEmailConfirmResult;
            }

            await _publish.Publish<UserSignedUp>(new
            {
                UserId = userToAdd.Id,
                Email = userToAdd.Email,
                FirstName = userToAdd.FirstName,
                LastName = userToAdd.LastName,
                __CorrelationId = InVar.Id,
            });
            return ApiResponse.Success();
        }


        public async Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return new ApiResponse(
                    new ApiError()
                    {
                        ErrorCode = ErrorCode.EmailConfirmationFailed,
                        ErrorMessage = ErrorCode.EmailConfirmationFailed.GetDescription(),
                    });
            }
            return await _emailHelperService.SendResetPasswordConfirmationEmail(user);
        }


        public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {

            var userFoundedResponse = await FindUserByIdAsync(request.UserId);
            if (!userFoundedResponse.IsSuccess)
            {
                return userFoundedResponse;
            }
            var result =
                await _userManager.ResetPasswordAsync(userFoundedResponse.Data, WebUtility.UrlDecode(request.Code), request.Password);
            if (result.Succeeded)
            {
                return ApiResponse.Success();
            }

            _logger.LogWarning($"Reset password for user {request.UserId} failed for reason {result.ToString()}");
            return new ApiResponse(
                new ApiError()
                {
                    ErrorCode = ErrorCode.ResetPasswordFailed,
                    ErrorMessage = ErrorCode.ResetPasswordFailed.GetDescription(),
                });
        }

        public async Task<ApiResponse> ResentEmailAsync(ResendEmailRequest request)
        {

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new ApiResponse<User>(new ApiError()
                {
                    ErrorCode = ErrorCode.UserNotFound,
                    ErrorMessage = ErrorCode.UserNotFound.GetDescription(),
                });
            }
            return await _emailHelperService.SendEmailConfirmationEmail(user);
        }


        private User ConstructUserModelToAdd(RegistrationRequest model)
        {
            return new User
            {
                UserName = model.Username,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                RegistrationDate = _systemClock.UtcNow,
                LockoutEnabled = true,
                LockoutEnd = null,
            };
        }

        private async Task<ApiResponse<User>> FindUserByIdAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("Could not find user by id {0}", userId);
                return new ApiResponse<User>(new ApiError()
                {
                    ErrorCode = ErrorCode.UserNotFound,
                    ErrorMessage = ErrorCode.UserNotFound.GetDescription(),
                });
            }
            return new ApiResponse<User>(user);
        }

        public async Task<ApiResponse> ConfirmEmailAsync(EmailConfirmationRequest request)
        {
            var userFoundedResponse = await FindUserByIdAsync(request.UserId);
            if (!userFoundedResponse.IsSuccess)
            {
                return userFoundedResponse;
            }

            var result = await _userManager.ConfirmEmailAsync(userFoundedResponse.Data, request.Code);
            if (result.Succeeded)
            {

                var addToRoleAsync = await _userManager.AddToRoleAsync(userFoundedResponse.Data, AuthorizationData.DefaultRole.ToString());
                if (!addToRoleAsync.Succeeded)
                {
                    _logger.LogError("Failed to add user to to role with reason {0}", addToRoleAsync.ToString());
                    return new ApiResponse(
                        new ApiError()
                        {
                            ErrorCode = ErrorCode.AddUserToRoleFailed,
                            ErrorMessage = ErrorCode.AddUserToRoleFailed.GetDescription(),
                        });
                }
                return ApiResponse.Success();
            }

            _logger.LogWarning("Email confirmation failed for user with id {0} by reason {1}",
                request.UserId, result.Errors.ToString());
            return new ApiResponse()
            {
                ResponseException = new ApiError()
                {
                    ErrorCode = ErrorCode.EmailConfirmationFailed,
                    ErrorMessage = ErrorCode.EmailConfirmationFailed.GetDescription(),
                },
                StatusCode = 400,
                IsSuccess = false,
            };
        }

        public async Task<ApiResponse<List<UserDto>>> GetAllUsers()
        {
            return new ApiResponse<List<UserDto>>(
                (await _userRepository.GetAllAsync())
                .Select(user => _userMapper.MapToModel(user)).ToList());
        }

        public async Task<ApiResponse<UserDto>> GetUsersById(Guid userId)
        {
            var userFounded = await FindUserByIdAsync(userId);
            return !userFounded.IsSuccess ? userFounded.ToFailed<UserDto>()
                : new ApiResponse<UserDto>(_userMapper.MapToModel(userFounded.Data));
        }
    }
}