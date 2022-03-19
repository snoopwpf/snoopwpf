namespace Snoop.Infrastructure;

using System;
using System.Collections.Generic;
using Snoop.Infrastructure.Diagnostics;
using Snoop.Infrastructure.Helpers;

public class CacheManager
{
    public static readonly CacheManager Instance = new();

    private CacheManager()
    {
        this.Participants.Add(BindingDiagnosticHelper.Instance);
        this.Participants.Add(SystemResourcesCache.Instance);
        this.Participants.Add(ResourceKeyCache.Instance);
    }

    public int UsageCount { get; private set; }

    public List<ICacheManaged> Participants { get; } = new();

    public void IncreaseUsageCount()
    {
        if (this.UsageCount == 0)
        {
            foreach (var participant in this.Participants)
            {
                participant.Activate();
            }
        }

        ++this.UsageCount;
    }

    public void DecreaseUsageCount()
    {
        --this.UsageCount;

        if (this.UsageCount == 0)
        {
            foreach (var participant in this.Participants)
            {
                participant.Dispose();
            }
        }
    }
}

public interface ICacheManaged : IDisposable
{
    void Activate();
}