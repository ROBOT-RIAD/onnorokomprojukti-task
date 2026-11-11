using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Exceptions;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Services;

public interface IAdmissionService
{
    Task<ClassCreatedResponseDto> CreateClassAsync(CreateClassRequest request);
    Task<List<ClassSummaryDto>> GetClassesAsync();
    Task<ClassSummaryDto?> GetClassByIdAsync(string classId);
    Task<HoldCreatedResponseDto> CreateHoldAsync(string classId, string studentId);
    Task<HoldStatusResponseDto?> GetHoldStatusAsync(string holdId);
    Task<HoldConfirmResponseDto> ConfirmHoldAsync(string holdId, string studentId);
    Task CancelHoldAsync(string holdId, string studentId);
    Task<List<MyHoldResponseDto>> GetStudentHoldsAsync(string studentId);
    Task CleanupExpiredHoldsAsync();
}

public class AdmissionService : IAdmissionService
{
    private readonly SchoolDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdmissionService> _logger;

    // In-memory semaphore dictionary fallback for concurrency safety when running in-memory unit tests
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _classLocks = new();

    public AdmissionService(
        SchoolDbContext dbContext,
        IConfiguration configuration,
        ILogger<AdmissionService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    private int GetHoldDurationSeconds()
    {
        var envValue = _configuration["HOLD_DURATION_SECONDS"];
        if (int.TryParse(envValue, out var seconds) && seconds > 0)
        {
            return seconds;
        }
        return 600; // default 10 minutes (600s)
    }

    public static void ValidateStudentId(string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId) || !Regex.IsMatch(studentId.Trim(), @"^\d{17}$"))
        {
            throw new BadInputException("X-Student-Id header must be a valid 17-digit birth registration number.");
        }
    }

    public async Task<ClassCreatedResponseDto> CreateClassAsync(CreateClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadInputException("Class name cannot be empty.");
        }

        if (request.TotalSeats <= 0)
        {
            throw new BadInputException("Total seats must be greater than 0.");
        }

        var classId = "c_" + Guid.NewGuid().ToString("N")[..8];
        var schoolClass = new SchoolClass
        {
            Id = classId,
            Name = request.Name.Trim(),
            TotalSeats = request.TotalSeats,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Classes.Add(schoolClass);
        await _dbContext.SaveChangesAsync();

        return new ClassCreatedResponseDto
        {
            Id = schoolClass.Id,
            Name = schoolClass.Name,
            TotalSeats = schoolClass.TotalSeats
        };
    }

    public async Task<List<ClassSummaryDto>> GetClassesAsync()
    {
        var now = DateTime.UtcNow;

        var classes = await _dbContext.Classes
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        var classIds = classes.Select(c => c.Id).ToList();

        // Get confirmed counts
        var confirmedCounts = await _dbContext.Holds
            .Where(h => classIds.Contains(h.ClassId) && h.Status == HoldStatus.Confirmed)
            .GroupBy(h => h.ClassId)
            .Select(g => new { ClassId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassId, x => x.Count);

        // Get active held counts (not expired)
        var heldCounts = await _dbContext.Holds
            .Where(h => classIds.Contains(h.ClassId) && h.Status == HoldStatus.Held && h.ExpiresAt > now)
            .GroupBy(h => h.ClassId)
            .Select(g => new { ClassId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassId, x => x.Count);

        return classes.Select(c =>
        {
            var confirmed = confirmedCounts.GetValueOrDefault(c.Id, 0);
            var held = heldCounts.GetValueOrDefault(c.Id, 0);
            var available = Math.Max(0, c.TotalSeats - (confirmed + held));

            return new ClassSummaryDto
            {
                Id = c.Id,
                Name = c.Name,
                TotalSeats = c.TotalSeats,
                HeldSeats = held,
                ConfirmedSeats = confirmed,
                AvailableSeats = available
            };
        }).ToList();
    }

    public async Task<ClassSummaryDto?> GetClassByIdAsync(string classId)
    {
        var schoolClass = await _dbContext.Classes.FindAsync(classId);
        if (schoolClass == null) return null;

        var now = DateTime.UtcNow;

        var confirmed = await _dbContext.Holds
            .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.Confirmed);

        var held = await _dbContext.Holds
            .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt > now);

        var available = Math.Max(0, schoolClass.TotalSeats - (confirmed + held));

        return new ClassSummaryDto
        {
            Id = schoolClass.Id,
            Name = schoolClass.Name,
            TotalSeats = schoolClass.TotalSeats,
            HeldSeats = held,
            ConfirmedSeats = confirmed,
            AvailableSeats = available
        };
    }

    public async Task<HoldCreatedResponseDto> CreateHoldAsync(string classId, string studentId)
    {
        ValidateStudentId(studentId);
        studentId = studentId.Trim();

        var sem = _classLocks.GetOrAdd(classId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();

        try
        {
            var isRelational = _dbContext.Database.IsRelational();
            var isNpgsql = _dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

            await using var tx = isRelational
                ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted)
                : null;

            try
            {
                if (isNpgsql)
                {
                    // PostgreSQL row lock on Classes row for this classId to guarantee cross-process serialization
                    await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT \"Id\" FROM \"Classes\" WHERE \"Id\" = {classId} FOR UPDATE");
                }

                var schoolClass = await _dbContext.Classes.FindAsync(classId);
                if (schoolClass == null)
                {
                    throw new ResourceNotFoundException($"Class with id '{classId}' was not found.");
                }

                var now = DateTime.UtcNow;

                // Rule 3: A student cannot have more than one active hold or confirmed admission in the same class at the same time
                var hasActive = await _dbContext.Holds.AnyAsync(h =>
                    h.ClassId == classId &&
                    h.StudentId == studentId &&
                    (h.Status == HoldStatus.Confirmed || (h.Status == HoldStatus.Held && h.ExpiresAt > now)));

                if (hasActive)
                {
                    throw new ConflictException("Student already has an active hold or confirmed admission in this class.");
                }

                // Check capacity
                var confirmedCount = await _dbContext.Holds
                    .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.Confirmed);

                var heldCount = await _dbContext.Holds
                    .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt > now);

                if (confirmedCount + heldCount >= schoolClass.TotalSeats)
                {
                    throw new ConflictException("No seats available in this class.");
                }

                var duration = GetHoldDurationSeconds();
                var expiresAt = now.AddSeconds(duration);

                var hold = new SeatHold
                {
                    HoldId = "h_" + Guid.NewGuid().ToString("N")[..8],
                    ClassId = classId,
                    StudentId = studentId,
                    Status = HoldStatus.Held,
                    CreatedAt = now,
                    ExpiresAt = expiresAt
                };

                _dbContext.Holds.Add(hold);
                await _dbContext.SaveChangesAsync();

                if (tx != null)
                {
                    await tx.CommitAsync();
                }

                return new HoldCreatedResponseDto
                {
                    HoldId = hold.HoldId,
                    ClassId = hold.ClassId,
                    StudentId = hold.StudentId,
                    Status = hold.Status,
                    ExpiresAt = hold.ExpiresAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
            }
            catch
            {
                if (tx != null)
                {
                    await tx.RollbackAsync();
                }
                throw;
            }
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<HoldStatusResponseDto?> GetHoldStatusAsync(string holdId)
    {
        var hold = await _dbContext.Holds.FindAsync(holdId);
        if (hold == null) return null;

        var now = DateTime.UtcNow;
        var status = hold.Status;

        if (status == HoldStatus.Held && hold.ExpiresAt <= now)
        {
            status = HoldStatus.Expired;
            hold.Status = HoldStatus.Expired;
            await _dbContext.SaveChangesAsync();
        }

        return new HoldStatusResponseDto
        {
            HoldId = hold.HoldId,
            ClassId = hold.ClassId,
            Status = status,
            ExpiresAt = hold.ExpiresAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    public async Task<HoldConfirmResponseDto> ConfirmHoldAsync(string holdId, string studentId)
    {
        ValidateStudentId(studentId);
        studentId = studentId.Trim();

        var isRelational = _dbContext.Database.IsRelational();
        var isNpgsql = _dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

        await using var tx = isRelational
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted)
            : null;

        try
        {
            if (isNpgsql)
            {
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT \"HoldId\" FROM \"Holds\" WHERE \"HoldId\" = {holdId} FOR UPDATE");
            }

            var hold = await _dbContext.Holds.FindAsync(holdId);
            if (hold == null)
            {
                throw new ResourceNotFoundException($"Hold with id '{holdId}' was not found.");
            }

            // Rule 4: Only own hold can be confirmed
            if (hold.StudentId != studentId)
            {
                throw new ForbiddenException("You cannot confirm a hold belonging to another student.");
            }

            // Rule 5: Idempotency - if already confirmed, return success without incrementing confirmed seats
            if (hold.Status == HoldStatus.Confirmed)
            {
                return new HoldConfirmResponseDto
                {
                    HoldId = hold.HoldId,
                    Status = HoldStatus.Confirmed
                };
            }

            var now = DateTime.UtcNow;

            // Must be HELD and not expired
            if (hold.Status != HoldStatus.Held || hold.ExpiresAt <= now)
            {
                if (hold.Status == HoldStatus.Held)
                {
                    hold.Status = HoldStatus.Expired;
                    await _dbContext.SaveChangesAsync();
                }
                throw new ConflictException("Hold has expired or is invalid. Cannot confirm admission.");
            }

            hold.Status = HoldStatus.Confirmed;
            hold.ConfirmedAt = now;
            await _dbContext.SaveChangesAsync();

            if (tx != null)
            {
                await tx.CommitAsync();
            }

            return new HoldConfirmResponseDto
            {
                HoldId = hold.HoldId,
                Status = HoldStatus.Confirmed
            };
        }
        catch
        {
            if (tx != null)
            {
                await tx.RollbackAsync();
            }
            throw;
        }
    }

    public async Task CancelHoldAsync(string holdId, string studentId)
    {
        ValidateStudentId(studentId);
        studentId = studentId.Trim();

        var hold = await _dbContext.Holds.FindAsync(holdId);
        if (hold == null)
        {
            throw new ResourceNotFoundException($"Hold with id '{holdId}' was not found.");
        }

        if (hold.StudentId != studentId)
        {
            throw new ForbiddenException("You cannot cancel a hold belonging to another student.");
        }

        if (hold.Status == HoldStatus.Confirmed)
        {
            throw new BadInputException("Cannot cancel an already confirmed admission.");
        }

        hold.Status = HoldStatus.Cancelled;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<MyHoldResponseDto>> GetStudentHoldsAsync(string studentId)
    {
        ValidateStudentId(studentId);
        studentId = studentId.Trim();

        var now = DateTime.UtcNow;

        var holds = await _dbContext.Holds
            .Include(h => h.Class)
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        return holds.Select(h =>
        {
            var status = h.Status;
            if (status == HoldStatus.Held && h.ExpiresAt <= now)
            {
                status = HoldStatus.Expired;
            }

            return new MyHoldResponseDto
            {
                HoldId = h.HoldId,
                ClassId = h.ClassId,
                ClassName = h.Class?.Name ?? string.Empty,
                Status = status,
                ExpiresAt = h.ExpiresAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }).ToList();
    }

    public async Task CleanupExpiredHoldsAsync()
    {
        var now = DateTime.UtcNow;
        var expired = await _dbContext.Holds
            .Where(h => h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .ToListAsync();

        if (expired.Count > 0)
        {
            foreach (var h in expired)
            {
                h.Status = HoldStatus.Expired;
            }
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired holds.", expired.Count);
        }
    }
}
