using System.Text.Json;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Helpers;
using MiCachito.Mobile.Models;
using MiCachito.Mobile.Models.Responses;
using Microsoft.Maui.Storage;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Persiste la sesión del expendio en SecureStorage como un único blob JSON
/// (SessionInfo, shape mobile Fase 1).
/// Implementa además <see cref="IAuthTokenProvider"/> para que ApiClient
/// inyecte el token Bearer sin conocer los detalles de almacenamiento.
/// Migración desde el shape Desktop (pre-Fase 1): el blob viejo deserializa
/// con Expendio/Billetero vacíos (campos desconocidos se ignoran); su token
/// Desktop es rechazado por api/mobile/auth/verify y SplashPage descarta la
/// sesión → Login. Ambos caminos terminan en Login limpio.
/// </summary>
public class SessionService : ISessionService, IAuthTokenProvider
{
    private static readonly JsonSerializerOptions StorageOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private SessionInfo? _currentSession;

    public SessionInfo? CurrentSession => _currentSession;

    public async Task SaveAsync(MobileLoginResponse loginResponse, string? dispositivo, bool rememberMe, CancellationToken cancellationToken = default)
    {
        var session = new SessionInfo
        {
            AccessToken = loginResponse.Token,
            Expendio = loginResponse.Expendio,
            Billetero = loginResponse.Billetero,
            Dispositivo = dispositivo,
            ExpiresAt = loginResponse.Expira,
            LoginAt = DateTime.UtcNow,
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
            // Sesión corrupta o de una versión anterior (shape Desktop): se descarta.
            await ClearAsync(cancellationToken).ConfigureAwait(false);
        }

        return _currentSession;
    }

    public async Task UpdateAsync(MobileVerifyResponse verifyResponse, CancellationToken cancellationToken = default)
    {
        var session = await LoadAsync(cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return;
        }

        session.Expendio = verifyResponse.Expendio;
        session.Billetero = verifyResponse.Billetero;
        if (verifyResponse.Sesion is not null)
        {
            session.IdSesion = verifyResponse.Sesion.IdSesion;
            session.Dispositivo = verifyResponse.Sesion.Dispositivo ?? session.Dispositivo;
        }

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
