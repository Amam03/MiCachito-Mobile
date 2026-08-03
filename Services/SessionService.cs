using System.Text.Json;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Helpers;
using MiCachito.Mobile.Models;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;
using Microsoft.Maui.Storage;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Persiste la sesión en SecureStorage como un único blob JSON (SessionInfo).
/// Implementa además <see cref="IAuthTokenProvider"/> para que ApiClient inyecte el token Bearer
/// sin conocer los detalles de almacenamiento.
/// </summary>
public class SessionService : ISessionService, IAuthTokenProvider
{
    private static readonly JsonSerializerOptions StorageOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private SessionInfo? _currentSession;

    public SessionInfo? CurrentSession => _currentSession;

    public async Task SaveAsync(LoginResponse loginResponse, bool rememberMe, CancellationToken cancellationToken = default)
    {
        var session = new SessionInfo
        {
            AccessToken = loginResponse.Token,
            Usuario = loginResponse.Usuario,
            LoginAt = DateTime.UtcNow,
            ExpiresAt = null,
            RememberMe = rememberMe,
        };

        await SaveSessionAsync(session).ConfigureAwait(false);
        _currentSession = session;
    }

    public async Task<SessionInfo?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_currentSession is not null)
        {
            return _currentSession;
        }

        var json = await SecureStorage.Default.GetAsync(StorageKeys.Session).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            _currentSession = JsonSerializer.Deserialize<SessionInfo>(json, StorageOptions);
        }
        catch (JsonException)
        {
            // Sesión corrupta o de una versión anterior: se descarta.
            await ClearAsync(cancellationToken).ConfigureAwait(false);
        }

        return _currentSession;
    }

    public async Task UpdateUsuarioAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        var session = await LoadAsync(cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return;
        }

        session.Usuario = usuario;
        await SaveSessionAsync(session).ConfigureAwait(false);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        SecureStorage.Default.Remove(StorageKeys.Session);
        _currentSession = null;
        return Task.CompletedTask;
    }

    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var session = await LoadAsync(cancellationToken).ConfigureAwait(false);
        return session?.AccessToken;
    }

    private static async Task SaveSessionAsync(SessionInfo session)
    {
        var json = JsonSerializer.Serialize(session, StorageOptions);
        await SecureStorage.Default.SetAsync(StorageKeys.Session, json).ConfigureAwait(false);
    }
}
