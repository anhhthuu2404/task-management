using System;
using Volo.Abp.Domain.Entities;

namespace TaskManagement.Departments;

public class UserDepartment : Entity
{
    public Guid UserId { get; set; }
    public Guid DepartmentId { get; set; }
    public bool IsManager { get; set; }

    public override object[] GetKeys()
    {
        return new object[] { UserId, DepartmentId };
    }
}