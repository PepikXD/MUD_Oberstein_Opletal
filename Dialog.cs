using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal;

public class DialogNode
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<DialogOption> Options { get; set; } = new();
}

public class DialogOption
{
    public string Text { get; set; } = string.Empty;
    public string NextNodeId { get; set; } = string.Empty;
    
    // Conditions
    public string RequiredQuest { get; set; } = string.Empty;
    public string RequiredQuestState { get; set; } = string.Empty; // e.g. "Active", "Completed"
    public string RequiredItem { get; set; } = string.Empty; // item ID

    // Actions
    public string SetQuestState { get; set; } = string.Empty; // e.g. "q1:Active"
    public string GiveItem { get; set; } = string.Empty;
    public string TakeItem { get; set; } = string.Empty;
}

public class DialogSession
{
    private readonly Player _player;
    private readonly NPC _npc;
    private DialogNode _currentNode;

    public DialogSession(Player player, NPC npc)
    {
        _player = player;
        _npc = npc;
        _currentNode = npc.DialogTree.TryGetValue(npc.StartingDialogNodeId, out var node) 
            ? node 
            : new DialogNode { Text = npc.Text }; // Fallback to basic text if no tree
    }

    public async Task StartAsync()
    {
        await PrintCurrentNodeAsync();
        if (_currentNode.Options.Count == 0)
        {
            EndSession();
        }
    }

    public async Task HandleInputAsync(string input)
    {
        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) || input.Equals("odejit", StringComparison.OrdinalIgnoreCase))
        {
            EndSession();
            await _player.Writer.WriteLineAsync("You ended the conversation.");
            return;
        }

        if (int.TryParse(input, out int index))
        {
            var validOptions = GetValidOptions();
            if (index >= 1 && index <= validOptions.Count)
            {
                var chosenOption = validOptions[index - 1];
                await ExecuteOptionAsync(chosenOption);
            }
            else
            {
                await _player.Writer.WriteLineAsync("Invalid option.");
            }
        }
        else
        {
            await _player.Writer.WriteLineAsync("Please enter a number, or 'exit' to leave.");
        }
    }

    private List<DialogOption> GetValidOptions()
    {
        var valid = new List<DialogOption>();
        foreach (var opt in _currentNode.Options)
        {
            // Check quest condition
            if (!string.IsNullOrEmpty(opt.RequiredQuest))
            {
                var state = _player.GetQuestState(opt.RequiredQuest).ToString();
                if (!state.Equals(opt.RequiredQuestState, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            // Check item condition
            if (!string.IsNullOrEmpty(opt.RequiredItem))
            {
                if (!_player.Inventory.Any(i => i.Id == opt.RequiredItem))
                    continue;
            }

            valid.Add(opt);
        }
        return valid;
    }

    private async Task ExecuteOptionAsync(DialogOption option)
    {
        // Handle actions
        if (!string.IsNullOrEmpty(option.SetQuestState))
        {
            var parts = option.SetQuestState.Split(':');
            if (parts.Length == 2 && Enum.TryParse<QuestState>(parts[1], true, out var state))
            {
                _player.SetQuestState(parts[0], state);
                await _player.Writer.WriteLineAsync($"[Quest Update] {parts[0]}: {state}");
            }
        }

        if (!string.IsNullOrEmpty(option.TakeItem))
        {
            var item = _player.Inventory.FirstOrDefault(i => i.Id == option.TakeItem);
            if (item != null)
                _player.RemoveFromInventory(item);
        }

        if (!string.IsNullOrEmpty(option.GiveItem))
        {
            var item = _player.Server.World.CreateItem(option.GiveItem);
            if (item != null)
                _player.AddToInventory(item);
        }

        if (string.IsNullOrEmpty(option.NextNodeId) || !_npc.DialogTree.TryGetValue(option.NextNodeId, out var nextNode))
        {
            EndSession();
            await _player.Writer.WriteLineAsync("Conversation ended.");
            return;
        }

        _currentNode = nextNode;
        await PrintCurrentNodeAsync();

        if (_currentNode.Options.Count == 0 || GetValidOptions().Count == 0)
        {
            EndSession();
            await _player.Writer.WriteLineAsync("Conversation ended.");
        }
    }

    private async Task PrintCurrentNodeAsync()
    {
        await _player.Writer.WriteLineAsync($"\n[{_npc.Name}]: {_currentNode.Text}");
        var validOptions = GetValidOptions();
        
        if (validOptions.Count > 0)
        {
            for (int i = 0; i < validOptions.Count; i++)
            {
                await _player.Writer.WriteLineAsync($"  {i + 1}. {validOptions[i].Text}");
            }
            await _player.Writer.WriteLineAsync("  (or type 'exit' to leave)");
        }
    }

    private void EndSession()
    {
        _player.ActiveDialog = null;
    }
}
