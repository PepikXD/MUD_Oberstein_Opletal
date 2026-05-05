using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal;

public class AccountData
{
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public List<string> InventoryItems { get; set; } = new();
    public int Currency { get; set; } = 100;
    public Dictionary<string, QuestState> Quests { get; set; } = new();
}

public class AccountManager
{
    private readonly string _accountsPath;

    public AccountManager(string accountsPath)
    {
        _accountsPath = accountsPath;
        if (!Directory.Exists(_accountsPath))
            Directory.CreateDirectory(_accountsPath);
    }

    private string GetAccountPath(string username) => Path.Combine(_accountsPath, $"{username.ToLower()}.json");

    public bool AccountExists(string username) => File.Exists(GetAccountPath(username));

    public async Task<AccountData?> LoadAccountAsync(string username)
    {
        string path = GetAccountPath(username);
        if (!File.Exists(path)) return null;

        string json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<AccountData>(json);
    }

    public async Task SaveAccountAsync(AccountData account)
    {
        string path = GetAccountPath(account.Name);
        string json = JsonSerializer.Serialize(account, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<AccountData> CreateAccountAsync(string username, string password, string startingRoomId)
    {
        var account = new AccountData
        {
            Name = username,
            Password = password,
            LocationId = startingRoomId,
            Currency = 100
        };
        await SaveAccountAsync(account);
        return account;
    }

    public async Task<bool> VerifyPasswordAsync(string username, string password)
    {
        var account = await LoadAccountAsync(username);
        if (account == null) return false;

        return account.Password == password;
    }
}
