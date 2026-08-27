using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace TaskManagement.Tasks
{
    public class TaskBackgroundWorker : AsyncPeriodicBackgroundWorkerBase, ITransientDependency
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public TaskBackgroundWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;

            // Thiết lập chu kỳ chạy ngầm: ví dụ 60 giây quét một lần (60000 ms)
            Timer.Period = 60000;
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            Logger.LogInformation("--- [Task Background Worker] Bắt đầu quét Task quá hạn và sinh Task lặp lại ---");

            using var scope = _serviceScopeFactory.CreateScope();
            var taskManager = scope.ServiceProvider.GetRequiredService<TaskManager>();

            // 1. Quét và cập nhật trạng thái Task quá hạn
            await taskManager.ProcessOverdueTasksAsync();

            // 2. Kiểm tra và sinh Task lặp lại tự động
            await taskManager.ProcessRecurringTasksAsync();

            Logger.LogInformation("--- [Task Background Worker] Hoàn thành chu kỳ quét ---");
        }
    }
}