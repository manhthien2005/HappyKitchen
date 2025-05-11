
using HappyKitchen.Models;
using Microsoft.EntityFrameworkCore;
using HappyKitchen.Data;
namespace HappyKitchen.Services
{
    public interface IQRCodeService
    {
        Task<List<QRCode>> GetAllQRCodesAsync();
        Task<QRCode> GetQRCodeByIdAsync(int id);
        Task<QRCode> GetQRCodeByTableIdAsync(int tableId);
        Task CreateQRCodeAsync(QRCode qrCode);
        Task UpdateQRCodeAsync(QRCode qrCode);
        Task DeleteQRCodeAsync(int id);
    }

    public class QRCodeService : IQRCodeService
    {
        private readonly ApplicationDbContext _context;

        public QRCodeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<QRCode>> GetAllQRCodesAsync()
        {
            return await _context.QRCodes
                .Include(q => q.Table)
                .ToListAsync();
        }

        public async Task<QRCode> GetQRCodeByIdAsync(int id)
        {
            return await _context.QRCodes
                .Include(q => q.Table)
                .FirstOrDefaultAsync(q => q.QRCodeID == id);
        }

        public async Task<QRCode> GetQRCodeByTableIdAsync(int tableId)
        {
            return await _context.QRCodes
                .Include(q => q.Table)
                .FirstOrDefaultAsync(q => q.TableID == tableId);
        }

        public async Task CreateQRCodeAsync(QRCode qrCode)
        {
            _context.QRCodes.Add(qrCode);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateQRCodeAsync(QRCode qrCode)
        {
            _context.Entry(qrCode).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteQRCodeAsync(int id)
        {
            var qrCode = await _context.QRCodes.FindAsync(id);
            if (qrCode != null)
            {
                _context.QRCodes.Remove(qrCode);
                await _context.SaveChangesAsync();
            }
        }
    }
}