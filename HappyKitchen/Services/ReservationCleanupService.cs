using HappyKitchen.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ReservationCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ReservationCleanupService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // Thay YourDbContext

                    // Lấy thời gian hiện tại (UTC) và chuyển sang UTC+7
                    var utcNow = DateTime.UtcNow;
                    var localTime = utcNow.AddHours(7); // UTC+7
                    Console.WriteLine($"Thời gian hiện tại (UTC): {utcNow}");
                    Console.WriteLine($"Thời gian hiện tại (UTC+7): {localTime}");

                    // Lấy tất cả các đơn có Status != 2
                    var reservations = await context.Reservations
                        .Where(r => r.Status != 2)
                        .ToListAsync(stoppingToken);

                    foreach (var reservation in reservations)
                    {
                        // Giả định CreatedTime là UTC+7
                        var minutesDiff = (localTime - reservation.CreatedTime).TotalMinutes;
                        Console.WriteLine($"ReservationID: {reservation.ReservationID}, CreatedTime: {reservation.CreatedTime}, Hiệu số phút: {minutesDiff}");

                        // So sánh với localTime (UTC+7)
                        if (minutesDiff > 15)
                        {
                            context.Reservations.Remove(reservation);
                            Console.WriteLine($"Đã đánh dấu xóa ReservationID: {reservation.ReservationID}");
                        }
                    }

                    // Lưu thay đổi
                    var removedCount = await context.SaveChangesAsync(stoppingToken);
                    if (removedCount > 0)
                    {
                        Console.WriteLine($"Đã xóa {removedCount} đơn đặt bàn hết hạn.");
                    }
                    else
                    {
                        Console.WriteLine("Không có đơn nào bị xóa.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi trong ReservationCleanupService: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}