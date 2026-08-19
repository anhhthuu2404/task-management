using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskManagement.LocalizationManagement.LanguageTexts;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Localization;
using Volo.Abp.Threading;

namespace TaskManagement.Localization
{
    // Bước 2 — Tạo Database Localization Contributor trong Application Layer - LanguageText
    public class DatabaseLocalizationContributor : ILocalizationResourceContributor
    {
        private readonly IRepository<LanguageText, Guid> _languageTextRepository;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly object _syncLock = new();

        private List<LanguageText>? _cachedTexts;
        private List<string>? _cachedCultures;

        public bool IsDynamic => true;

        public DatabaseLocalizationContributor(IRepository<LanguageText, Guid> languageTextRepository)
        {
            _languageTextRepository = languageTextRepository;
        }

        public void InvalidateCache()
        {
            _cachedTexts = null;
            _cachedCultures = null;
        }

        // ABP gọi khi khởi tạo contributor
        public void Initialize(LocalizationResourceInitializationContext context)
        {
            // Không cần xử lý ở scenario này
        }

        // Dùng để đọc 1 key cụ thể
        public LocalizedString? GetOrNull(string cultureName, string name)
        {
            EnsureCacheLoaded();

            var item = _cachedTexts?
                .FirstOrDefault(x =>
                    x.CultureName == cultureName &&
                    x.Key == name);

            if (item == null)
                return null;

            return new LocalizedString(name, item.Value);
        }

        // Đồng bộ hóa dữ liệu vào dictionary (ABP sẽ merge với JSON)
        public void Fill(string cultureName, Dictionary<string, LocalizedString> dictionary)
        {
            EnsureCacheLoaded();

            var texts = _cachedTexts?
                .Where(x => x.CultureName == cultureName)
                .ToList() ?? new List<LanguageText>();

            foreach (var t in texts)
            {
                dictionary[t.Key] = new LocalizedString(t.Key, t.Value);
            }
        }

        // Bản async (được ABP gọi trong nhiều trường hợp)
        public async Task FillAsync(string cultureName, Dictionary<string, LocalizedString> dictionary)
        {
            await EnsureCacheLoadedAsync();

            var texts = _cachedTexts?
                .Where(x => x.CultureName == cultureName)
                .ToList() ?? new List<LanguageText>();

            foreach (var t in texts)
            {
                dictionary[t.Key] = new LocalizedString(t.Key, t.Value);
            }
        }

        // Trả về danh sách ngôn ngữ hiện có trong DB
        public async Task<IEnumerable<string>> GetSupportedCulturesAsync()
        {
            await EnsureCacheLoadedAsync();
            return _cachedCultures ?? Enumerable.Empty<string>();
        }

        // ====== PRIVATE HELPERS ======

        private void EnsureCacheLoaded()
        {
            // 1. Nếu đã có Cache thì trả về ngay (Mở lại comment bị khóa)
            if (_cachedTexts != null)
            {
                return;
            }

            // 2. Sử dụng lock đồng bộ và AsyncHelper.RunSync của ABP để tránh Deadlock
            lock (_syncLock)
            {
                if (_cachedTexts == null)
                {
                    _cachedTexts = AsyncHelper.RunSync(() => _languageTextRepository.GetListAsync());
                    _cachedCultures = _cachedTexts
                        .Select(x => x.CultureName)
                        .Distinct()
                        .ToList();
                }
            }
        }

        private async Task EnsureCacheLoadedAsync()
        {
            // 1. Tránh load lại DB nếu đã có Cache
            if (_cachedTexts != null)
            {
                return;
            }

            // 2. Sử dụng SemaphoreSlim để đảm bảo chỉ 1 Request gọi DB tại một thời điểm
            await _semaphore.WaitAsync();
            try
            {
                if (_cachedTexts == null)
                {
                    _cachedTexts = await _languageTextRepository.GetListAsync();
                    _cachedCultures = _cachedTexts
                        .Select(x => x.CultureName)
                        .Distinct()
                        .ToList();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}