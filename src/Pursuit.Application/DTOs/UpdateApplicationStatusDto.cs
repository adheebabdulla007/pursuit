using Pursuit.Domain.Enums;

namespace Pursuit.Application.DTOs;

public class UpdateApplicationStatusDto
{
    public ApplicationStatus Status { get; set; }
}