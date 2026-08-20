using Microsoft.Extensions.DependencyInjection;
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
using Volo.Abp.Uow;

namespace TaskManagement.Localization;

public class DatabaseLocalizationContributor : ILocalizationResourceContributor
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _syncLock = new();

    private List<LanguageText>? _cachedTexts;
    private List<string>? _cachedCultures;

    public bool IsDynamic => true;

    public DatabaseLocalizationContributor(IServiceProvider serviceProvider)
    {
        _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    public void InvalidateCache()
    {
        _cachedTexts = null;
        _cachedCultures = null;
    }

    public void Initialize(LocalizationResourceInitializationContext context)
    {
    }

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

    public void Fill(string cultureName, Dictionary<string, LocalizedString> dictionary)
    {
        EnsureCacheLoaded();

        var texts = _cachedTexts?
            .Where(x => x.CultureName == cultureName)
            .ToList() ?? [];

        foreach (var t in texts)
        {
            dictionary[t.Key] = new LocalizedString(t.Key, t.Value);
        }
    }

    public async Task FillAsync(string cultureName, Dictionary<string, LocalizedString> dictionary)
    {
        await EnsureCacheLoadedAsync();

        var texts = _cachedTexts?
            .Where(x => x.CultureName == cultureName)
            .ToList() ?? [];

        foreach (var t in texts)
        {
            dictionary[t.Key] = new LocalizedString(t.Key, t.Value);
        }
    }

    public async Task<IEnumerable<string>> GetSupportedCulturesAsync()
    {
        await EnsureCacheLoadedAsync();
        return _cachedCultures ?? Enumerable.Empty<string>();
    }

    private void EnsureCacheLoaded()
    {
        if (_cachedTexts != null)
        {
            return;
        }

        lock (_syncLock)
        {
            if (_cachedTexts == null)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

                // 🟢 Đặt requiresNew: true để cách ly hoàn toàn UOW
                using var uow = uowManager.Begin(requiresNew: true);
                var languageTextRepository = scope.ServiceProvider.GetRequiredService<IRepository<LanguageText, Guid>>();

                _cachedTexts = AsyncHelper.RunSync(() => languageTextRepository.GetListAsync());
                _cachedCultures = _cachedTexts
                    .Select(x => x.CultureName)
                    .Distinct()
                    .ToList();

                AsyncHelper.RunSync(() => uow.CompleteAsync());
            }
        }
    }

    private async Task EnsureCacheLoadedAsync()
    {
        if (_cachedTexts != null)
        {
            return;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (_cachedTexts == null)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

                using var uow = uowManager.Begin(requiresNew: true);
                var languageTextRepository = scope.ServiceProvider.GetRequiredService<IRepository<LanguageText, Guid>>();

                _cachedTexts = await languageTextRepository.GetListAsync();
                _cachedCultures = _cachedTexts
                    .Select(x => x.CultureName)
                    .Distinct()
                    .ToList();

                await uow.CompleteAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}