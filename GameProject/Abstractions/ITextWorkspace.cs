using GameProject.TextModel;

namespace GameProject.Abstractions;

/// <summary>
/// Contract for document navigation and editing. Another implementation
/// (for example, a file-backed workspace) can replace TextWorkspace.
/// </summary>
public interface ITextWorkspace
{
    TextNode Root { get; }
    TextNode Current { get; }
    string CurrentPath { get; }
    TextNode Add(TextNodeKind kind, string name, string content = "");
    void MoveUp();
    void ChangeDirectory(string path);
    void ChangeDirectoryById(string id);
    TextNode? FindById(TextNode node, string id);
    void PrintCurrent(bool includeIds = false);
    void PrintWhole(bool includeIds = false);
    void RemoveCurrent();
    void RemoveChild(string name);
}
