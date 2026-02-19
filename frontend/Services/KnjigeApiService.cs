using System.Text.Json.Serialization;

namespace BlazorApp1.Services;

public class KnjigaDto
{
    public int Id { get; set; }
    public string Naziv { get; set; } = "";
    public string Autor { get; set; } = "";
    public string ISBN { get; set; } = "";
    public List<KnjiznicaKnjigaDto> KnjiznicaKnjige { get; set; } = new();
}

public class KnjiznicaKnjigaDto
{
    public int Id { get; set; }
    public int KnjiznicaId { get; set; }
    public int KnjigaId { get; set; }
    public KnjiznicaDto? Knjiznica { get; set; }
}

public class KnjiznicaDto
{
    public int Id { get; set; }
    public string Naziv { get; set; } = "";
}

public class KnjigeApiService
{
    private readonly HttpClient _http;

    public KnjigeApiService(HttpClient http) => _http = http;

    public async Task<List<KnjigaDto>> GetSveKnjigeAsync() =>
        await _http.GetFromJsonAsync<List<KnjigaDto>>("api/knjige") ?? [];

    public async Task<KnjigaDto?> GetKnjigaAsync(int id) =>
        await _http.GetFromJsonAsync<KnjigaDto>($"api/knjige/{id}");

    public async Task<List<KnjiznicaDto>> GetKnjizniceAsync() =>
        await _http.GetFromJsonAsync<List<KnjiznicaDto>>("api/knjiznice") ?? [];

    public async Task<(bool ok, string? error)> DodajKnjiguAsync(string naziv, string autor, string isbn, List<int> knjizniceIds)
    {
        var body = new KnjigaRequestDto { Naziv = naziv, Autor = autor, ISBN = isbn, KnjizniceIds = knjizniceIds };
        var resp = await _http.PostAsJsonAsync("api/knjige", body);
        if (resp.IsSuccessStatusCode) return (true, null);
        var err = await resp.Content.ReadAsStringAsync();
        return (false, err.Length > 200 ? resp.ReasonPhrase : err);
    }

    public async Task<(bool ok, string? error)> UrediKnjiguAsync(int id, string naziv, string autor, string isbn, List<int> knjizniceIds)
    {
        var body = new KnjigaRequestDto { Naziv = naziv, Autor = autor, ISBN = isbn, KnjizniceIds = knjizniceIds };
        var resp = await _http.PutAsJsonAsync($"api/knjige/{id}", body);
        if (resp.IsSuccessStatusCode) return (true, null);
        var err = await resp.Content.ReadAsStringAsync();
        return (false, err.Length > 200 ? resp.ReasonPhrase : err);
    }

    public async Task<(bool ok, string? error)> IzbrisiKnjiguAsync(int id)
    {
        var resp = await _http.DeleteAsync($"api/knjige/{id}");
        if (resp.IsSuccessStatusCode) return (true, null);
        var body = await resp.Content.ReadAsStringAsync();
        return (false, body.Length > 200 ? resp.ReasonPhrase : body);
    }
}

internal class KnjigaRequestDto
{
    [JsonPropertyName("naziv")]
    public string Naziv { get; set; } = "";
    [JsonPropertyName("autor")]
    public string Autor { get; set; } = "";
    [JsonPropertyName("isbn")]
    public string ISBN { get; set; } = "";
    [JsonPropertyName("knjizniceIds")]
    public List<int>? KnjizniceIds { get; set; }
}
