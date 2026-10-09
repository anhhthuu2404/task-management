using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TaskManagement.Notifications;
using TaskManagement.Provider.Interface;
using TaskManagement.Provider.Response;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using GTranslate.Translators;

namespace TaskManagement.Notifications
{
    public class NotificationAppService : ApplicationService, INotificationAppService
    {
        private readonly INotificationProvider _notificationProvider;
        private readonly INotificationNotifier _notificationNotifier;
        private readonly IdentityUserManager _identityUserManager;
        private readonly ITranslator _translator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationAppService(
            INotificationProvider notificationProvider,
            INotificationNotifier notificationNotifier,
            IdentityUserManager identityUserManager,
            ITranslator translator,
            IHttpContextAccessor httpContextAccessor)
        {
            _notificationProvider = notificationProvider;
            _notificationNotifier = notificationNotifier;
            _identityUserManager = identityUserManager;
            _translator = translator;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync()
        {
            if (!CurrentUser.Id.HasValue) return new List<NotificationDto>();

            var userId = CurrentUser.Id.Value;
            var notifications = await _notificationProvider.GetByUserIdAsync(userId);

            // Kiểm tra ngôn ngữ từ HTTP Header chuẩn của Angular gửi lên
            bool isEnglish = false;
            if (_httpContextAccessor?.HttpContext?.Request != null)
            {
                var acceptLang = _httpContextAccessor.HttpContext.Request.Headers["Accept-Language"].ToString();
                var abpCulture = _httpContextAccessor.HttpContext.Request.Headers["Abp-Culture"].ToString();
                string currentLang = !string.IsNullOrEmpty(abpCulture) ? abpCulture : acceptLang;

                if (!string.IsNullOrEmpty(currentLang) && currentLang.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    isEnglish = true;
                }
            }

            var notificationDtos = new List<NotificationDto>();

            foreach (var x in notifications)
            {
                string displayMessage = string.Empty;

                if (isEnglish)
                {
                    // Ưu tiên 1: Lấy MessageEn nếu có và không phải là chuỗi ID rác
                    if (!string.IsNullOrEmpty(x.MessageEn) && !Guid.TryParse(x.MessageEn, out _))
                    {
                        displayMessage = x.MessageEn;
                    }
                    else if (!string.IsNullOrEmpty(x.Message) && !Guid.TryParse(x.Message, out _))
                    {
                        // Ưu tiên 2: Fallback dịch nóng sang tiếng Anh nếu chưa có MessageEn
                        try
                        {
                            var translationResult = await _translator.TranslateAsync(x.Message, "en");
                            if (translationResult != null && !string.IsNullOrEmpty(translationResult.Translation))
                            {
                                displayMessage = translationResult.Translation;
                            }
                            else
                            {
                                displayMessage = x.Message;
                            }
                        }
                        catch
                        {
                            displayMessage = x.Message;
                        }
                    }
                    else
                    {
                        displayMessage = "You have a new task update";
                    }
                }
                else
                {
                    // Chế độ Tiếng Việt: Ưu tiên lấy x.Message nếu hợp lệ
                    if (!string.IsNullOrEmpty(x.Message) && !Guid.TryParse(x.Message, out _))
                    {
                        // Kiểm tra phòng hờ dữ liệu cũ lưu bằng tiếng Anh thì dịch ngược lại sang tiếng Việt
                        // Nhận biết tiếng Anh đơn giản qua ký tự hoặc từ khóa, hoặc chủ động dịch nóng nếu cần
                        displayMessage = x.Message;
                    }
                    else if (!string.IsNullOrEmpty(x.MessageEn) && !Guid.TryParse(x.MessageEn, out _))
                    {
                        // Nếu x.Message bị trống nhưng có MessageEn (dữ liệu cũ), dịch ngược từ MessageEn sang Tiếng Việt
                        try
                        {
                            var translationResult = await _translator.TranslateAsync(x.MessageEn, "vi");
                            if (translationResult != null && !string.IsNullOrEmpty(translationResult.Translation))
                            {
                                displayMessage = translationResult.Translation;
                            }
                            else
                            {
                                displayMessage = "Bạn có một cập nhật mới về công việc";
                            }
                        }
                        catch
                        {
                            displayMessage = "Bạn có một cập nhật mới về công việc";
                        }
                    }
                    else
                    {
                        displayMessage = "Bạn có một cập nhật mới về công việc";
                    }
                }

                notificationDtos.Add(new NotificationDto
                {
                    Id = x.Id,
                    Message = displayMessage,
                    IsRead = x.IsRead,
                    CreationTime = x.CreationTime,
                    TaskId = x.TaskId
                });
            }

            var distinctNotifications = notificationDtos
                .GroupBy(n => n.Id)
                .Select(g => g.First())
                .OrderByDescending(x => x.CreationTime)
                .ToList();

            return distinctNotifications;
        }

        public async Task CreateTaskNotificationAsync(Guid? assignedUserId, Guid taskId, string message)
        {
            // Lớp bảo vệ chống lưu chuỗi ID (GUID) vào thông báo
            if (string.IsNullOrEmpty(message) || Guid.TryParse(message, out _))
            {
                message = "Bạn có một cập nhật mới về công việc";
            }

            var currentUserId = CurrentUser.Id;
            var targetUserIds = new HashSet<Guid>();

            var adminUsers = await _identityUserManager.GetUsersInRoleAsync("admin");
            foreach (var admin in adminUsers)
            {
                if (admin.Id != currentUserId)
                {
                    targetUserIds.Add(admin.Id);
                }
            }

            if (assignedUserId.HasValue && assignedUserId.Value != currentUserId)
            {
                targetUserIds.Add(assignedUserId.Value);
            }

            // Tiến hành dịch sang tiếng Anh khi tạo một cách an toàn
            string messageEn = message;
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    var translationResult = await _translator.TranslateAsync(message, "en");
                    if (translationResult != null && !string.IsNullOrEmpty(translationResult.Translation))
                    {
                        messageEn = translationResult.Translation;
                    }
                }
            }
            catch (Exception)
            {
                messageEn = "You have a new task update";
            }

            foreach (var targetId in targetUserIds)
            {
                var notificationResponse = new NotificationQueryResponse
                {
                    Id = GuidGenerator.Create(),
                    UserId = targetId,
                    Message = message,     // Lưu bản tiếng Việt chuẩn
                    MessageEn = messageEn, // Lưu bản tiếng Anh đã dịch
                    IsRead = false,
                    CreationTime = Clock.Now,
                    CreatorId = currentUserId,
                    TaskId = taskId
                };

                await _notificationProvider.CreateAsync(notificationResponse);
                await _notificationNotifier.SendClientNotificationAsync(targetId, notificationResponse);
            }
        }

        public async Task CreateNotificationAsync(Guid targetReviewerId, Guid taskId, string message)
        {
            await CreateTaskNotificationAsync(targetReviewerId, taskId, message);
        }

        public async Task MarkAsReadAsync(Guid id)
        {
            await _notificationProvider.MarkAsReadAsync(id);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _notificationProvider.MarkAsDeletedAsync(id);
        }
    }
}