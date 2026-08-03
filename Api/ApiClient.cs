using System.Net;
using System.Text;
using System.Text.Json;
using MiCachito.Mobile.Helpers;
using MiCachito.Mobile.Models.Common;

namespace MiCachito.Mobile.Api;

/// <summary>
/// Cliente HTTP tipado contra el backend Yii2.
/// Se registra con HttpClientFactory (AddHttpClient) para gestionar su ciclo de vida.
/// Añade el header Authorization: Bearer automáticamente vía <see cref="IAuthTokenProvider"/>,
/// serializa en snake_case y devuelve el campo "data" deserializado.
/// Lanza <see cref="ApiException"/> en HTTP no 2xx o cuando el backend responde success=false.
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly IAuthTokenProvider _tokenProvider;

    public ApiClient(HttpClient http, IAuthTokenProvider tokenProvider)
    {
        _http = http;
        _tokenProvider = tokenProvider;
    }

    public async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default)
        => await SendAsync<T>(HttpMethod.Get, path, null, cancellationToken).ConfigureAwait(false);

    public async Task<T?> PostAsync<T>(string path, object? body = null, CancellationToken cancellationToken = default)
        => await SendAsync<T>(HttpMethod.Post, path, body, cancellationToken).ConfigureAwait(false);

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(Constants.JsonContentType));

        var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                Constants.BearerScheme,
                token);
        }

        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, ApiJsonOptions.Default),
                Encoding.UTF8,
                Constants.JsonContentType);
        }

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorEnvelope = DeserializeEnvelope<object>(responseBody, response.StatusCode);
            throw new ApiException(
                response.StatusCode,
                errorEnvelope?.Error ?? errorEnvelope?.Message,
                errorEnvelope?.Errors);
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return default;
        }

        var apiResponse = DeserializeEnvelope<T>(responseBody, response.StatusCode);
        if (apiResponse is null || !apiResponse.Success)
        {
            throw new ApiException(
                response.StatusCode,
                apiResponse?.Error ?? apiResponse?.Message,
                apiResponse?.Errors);
        }

        return apiResponse.Data;
    }

    private static ApiResponse<T>? DeserializeEnvelope<T>(string body, HttpStatusCode statusCode)
    {
        try
        {
            return JsonSerializer.Deserialize<ApiResponse<T>>(body, ApiJsonOptions.Default);
        }
        catch (JsonException ex)
        {
            throw new ApiException(statusCode, "El servidor devolvió una respuesta no válida.", null, ex);
        }
    }
}
