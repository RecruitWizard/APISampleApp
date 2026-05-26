using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace RecruitWizard.AuthSample.Services;

/// <summary>
/// Tracks <c>state</c> values issued by /auth/login and validates them on
/// /auth/callback to mitigate CSRF. States expire after 10 minutes and can
/// only be consumed once.
/// </summary>
public sealed class StateStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _pending = new();

    public string Generate()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        var state = Convert.ToHexString(bytes).ToLowerInvariant();
        _pending[state] = DateTimeOffset.UtcNow + Ttl;
        Sweep();
        return state;
    }

    public bool Consume(string? state)
    {
        if (string.IsNullOrWhiteSpace(state)) return false;
        if (!_pending.TryRemove(state, out var expiresAt)) return false;
        return expiresAt >= DateTimeOffset.UtcNow;
    }

    private void Sweep()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kvp in _pending)
        {
            if (kvp.Value < now) _pending.TryRemove(kvp.Key, out _);
        }
    }
}
