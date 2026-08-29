namespace TodoListAPI.Contracts;

public record CreateListRequest(string Title);

public record AddTaskRequest(string Title);

public record TodoTaskResponse(
    int TodoTaskId,
    string Title,
    bool Status);

public record TodoListResponse(
    int TodoListId,
    string Title,
    IReadOnlyCollection<TodoTaskResponse> Tasks);