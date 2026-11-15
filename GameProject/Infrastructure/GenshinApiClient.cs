using System.Net.Http;
using System.Text.Json;

namespace GameProject.Infrastructure;

public sealed record RemoteAbility(string Name, string Description);
public sealed record RemoteCharacter(string Name, string Description, int Rarity, string Element, string WeaponType, IReadOnlyList<RemoteAbility> Abilities);
public sealed record RemoteWeapon(string Name, string Type, int Rarity, int BaseAttack, string Description);

/// <summary>HTTP adapter for the public Genshin API (https://genshin.jmp.blue).</summary>
public sealed class GenshinApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsClient;
    private readonly string _baseUrl;
    public GenshinApiClient(HttpClient? httpClient = null, string baseUrl = "https://genshin.jmp.blue")
    { _http = httpClient ?? new HttpClient(); _ownsClient = httpClient is null; _baseUrl = baseUrl.TrimEnd('/'); _http.Timeout = TimeSpan.FromSeconds(20); }
    public async Task<IReadOnlyList<string>> GetCharacterNamesAsync(CancellationToken token = default) => await GetStringListAsync("/characters", token);
    public async Task<IReadOnlyList<string>> GetWeaponNamesAsync(CancellationToken token = default) => await GetStringListAsync("/weapons", token);
    public async Task<RemoteCharacter> GetCharacterAsync(string name, CancellationToken token = default)
    {
        using var doc = await GetJsonAsync($"/characters/{Uri.EscapeDataString(ToSlug(name))}", token); var root = doc.RootElement;
        var abilities = new List<RemoteAbility>();
        if (root.TryGetProperty("skillTalents", out var talents) && talents.ValueKind == JsonValueKind.Array)
            foreach (var x in talents.EnumerateArray()) abilities.Add(new(ReadString(x,"name","Unnamed ability"), ReadString(x,"description","")));
        if (abilities.Count == 0 && root.TryGetProperty("passiveTalents", out var passives) && passives.ValueKind == JsonValueKind.Array)
            foreach (var x in passives.EnumerateArray()) abilities.Add(new(ReadString(x,"name","Passive"), ReadString(x,"description","")));
        return new(ReadString(root,"name",name), ReadString(root,"description","No description provided."), ReadInt(root,"rarity",4), ReadString(root,"vision","Unknown"), ReadString(root,"weapon","Unknown"), abilities);
    }
    public async Task<RemoteWeapon> GetWeaponAsync(string name, CancellationToken token = default)
    {
        using var doc = await GetJsonAsync($"/weapons/{Uri.EscapeDataString(ToSlug(name))}", token); var root = doc.RootElement;
        return new(ReadString(root,"name",name), ReadString(root,"type","Unknown"), ReadInt(root,"rarity",3), ReadInt(root,"baseAttack",0), ReadString(root,"passiveDesc",ReadString(root,"description","")));
    }
    private async Task<IReadOnlyList<string>> GetStringListAsync(string path, CancellationToken token)
    { using var doc = await GetJsonAsync(path, token); if(doc.RootElement.ValueKind != JsonValueKind.Array) throw new InvalidDataException($"Unexpected API response for {path}."); return doc.RootElement.EnumerateArray().Where(x=>x.ValueKind==JsonValueKind.String).Select(x=>x.GetString()!).ToArray(); }
    private async Task<JsonDocument> GetJsonAsync(string path, CancellationToken token)
    { using var response=await _http.GetAsync(_baseUrl+path,token); response.EnsureSuccessStatusCode(); await using var stream=await response.Content.ReadAsStreamAsync(token); return await JsonDocument.ParseAsync(stream,cancellationToken:token); }
    private static string ToSlug(string value)=>value.Trim().ToLowerInvariant().Replace(" ","-").Replace("'","");
    private static string ReadString(JsonElement e,string n,string fallback)=>e.TryGetProperty(n,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??fallback:fallback;
    private static int ReadInt(JsonElement e,string n,int fallback)=>e.TryGetProperty(n,out var v)&&v.TryGetInt32(out var x)?x:fallback;
    public void Dispose(){if(_ownsClient)_http.Dispose();}
}
