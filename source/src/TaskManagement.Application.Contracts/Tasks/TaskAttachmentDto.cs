using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Tasks;

public class TaskAttachmentDto
{
    public string FileName { get; set; } = default!;
    public string FileContent { get; set; } = default!;
}