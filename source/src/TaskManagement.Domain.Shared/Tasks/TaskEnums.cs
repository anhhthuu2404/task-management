namespace TaskManagement.Tasks;

public enum TaskItemStatus
{
    New = 0,
    InProgress = 1,
    InReview = 2,
    Completed = 3,
    Canceled = 4,
    Overdue = 5 
}
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

public enum TaskItemPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

public enum RecurrenceFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}