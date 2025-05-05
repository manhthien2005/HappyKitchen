using HappyKitchen.Attributes;
using HappyKitchen.Models;
using HappyKitchen.Services;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HappyKitchen.Controllers
{
    public class QRCodeManageController : Controller
    {
        private readonly IQRCodeService _qrCodeService;
        private readonly ITableService _tableService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<QRCodeManageController> _logger;

        public QRCodeManageController(
            IQRCodeService qrCodeService,
            ITableService tableService,
            IWebHostEnvironment environment,
            ILogger<QRCodeManageController> logger)
        {
            _qrCodeService = qrCodeService;
            _tableService = tableService;
            _environment = environment;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
    
        [HttpGet]
        public async Task<IActionResult> GetTables()
        {
            _logger.LogDebug("[API] GettingTables");
            
            try
            {
                var categories = await _tableService.GetAllTablesAsync();
                
                return Json(new
                {
                    success = true,
                    data = categories.Select(c => new
                    {
                        c.TableID,
                        c.TableName
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lấy danh sách bàn" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetQRCodes(
            int page = 1,
            int pageSize = 8,
            string searchTerm = "",
            string status = "all",
            string sortBy = "table_asc")
        {
            try
            {
                var qrCodes = await _qrCodeService.GetAllQRCodesAsync();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    qrCodes = qrCodes.Where(q =>
                        q.Table.TableName.ToString().Contains(searchTerm) ||
                        q.MenuUrl.ToLower().Contains(searchTerm)
                    ).ToList();
                }

                if (status != "all")
                {
                    byte statusValue = byte.Parse(status);
                    qrCodes = qrCodes.Where(q => q.Status == statusValue).ToList();
                }

                qrCodes = sortBy switch
                {
                    "table_asc" => qrCodes.OrderBy(q => q.Table.TableName).ToList(),
                    "table_desc" => qrCodes.OrderByDescending(q => q.Table.TableName).ToList(),
                    "access_asc" => qrCodes.OrderBy(q => q.AccessCount).ToList(),
                    "access_desc" => qrCodes.OrderByDescending(q => q.AccessCount).ToList(),
                    _ => qrCodes.OrderBy(q => q.Table.TableName).ToList()
                };

                int totalItems = qrCodes.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

                var pagedQRCodes = qrCodes
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = pagedQRCodes.Select(q => new
                    {
                        q.QRCodeID,
                        q.Table.TableName,
                        q.Table.TableID,
                        q.QRCodeImage,
                        q.MenuUrl,
                        q.AccessCount,
                        q.Status,
                        q.CreatedAt
                    }),
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = totalItems,
                        totalPages = totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving QR codes");
                return Json(new { success = false, message = "Error retrieving QR codes" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> CreateQRCode([FromBody] QRCodeCreateModel model)
        {
            try
            {
                if (model.TableID <= 0)
                    return Json(new { success = false, message = "Valid table is required" });

                var table = await _tableService.GetTableByIdAsync(model.TableID);
                if (table == null)
                    return Json(new { success = false, message = "Không tìm thấy bàn này" });

                var existingQR = await _qrCodeService.GetQRCodeByTableIdAsync(model.TableID);
                if (existingQR != null)
                    return Json(new { success = false, message = "QR Code cho bàn này đã tồn tại" });

                // Generate menu URL
                var menuUrl = $"https://{HttpContext.Request.Host}/Menu?tableId={model.TableID}";

                // Generate QR code
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(menuUrl, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);

                var fileName = $"{Guid.NewGuid()}.png";
                var filePath = Path.Combine(_environment.WebRootPath, "images", "QRCodes", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                await System.IO.File.WriteAllBytesAsync(filePath, qrCodeBytes);

                var qrCodeModel = new Models.QRCode
                {
                    TableID = model.TableID,
                    QRCodeImage = fileName,
                    MenuUrl = menuUrl,
                    Status = 1
                };

                await _qrCodeService.CreateQRCodeAsync(qrCodeModel);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating QR code");
                return Json(new { success = false, message = "Error creating QR code" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateQRCode([FromBody] QRCodeUpdateModel model)
        {
            try
            {
                if (model.QRCodeID <= 0)
                    return Json(new { success = false, message = "Valid QR code ID is required" });

                var qrCode = await _qrCodeService.GetQRCodeByIdAsync(model.QRCodeID);
                if (qrCode == null)
                    return Json(new { success = false, message = "QR code not found" });

                var table = await _tableService.GetTableByIdAsync(model.TableID);
                if (table == null)
                    return Json(new { success = false, message = "Table not found" });

                var existingQR = await _qrCodeService.GetQRCodeByTableIdAsync(model.TableID);
                if (existingQR != null && existingQR.QRCodeID != model.QRCodeID)
                    return Json(new { success = false, message = "QR code already exists for this table" });


                // Regenerate QR code if table changed
                if (qrCode.TableID != model.TableID)
                {
                    var menuUrl = $"https://{HttpContext.Request.Host}/Menu?tableId={model.TableID}";
                    
                    using var qrGenerator = new QRCodeGenerator();
                    var qrCodeData = qrGenerator.CreateQrCode(menuUrl, QRCodeGenerator.ECCLevel.Q);
                    using var qrCodeG = new PngByteQRCode(qrCodeData);
                    var qrCodeBytes = qrCodeG.GetGraphic(20);

                    var fileName = $"{Guid.NewGuid()}.png";
                    var filePath = Path.Combine(_environment.WebRootPath, "images", "QRCodes", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    await System.IO.File.WriteAllBytesAsync(filePath, qrCodeBytes);
                    // Delete old image
                    if (!string.IsNullOrEmpty(qrCode.QRCodeImage))
                    {
                        var oldImagePath = Path.Combine(_environment.WebRootPath, "images", "QRCodes", qrCode.QRCodeImage);
                        if (System.IO.File.Exists(oldImagePath))
                            System.IO.File.Delete(oldImagePath);
                    }

                    qrCode.QRCodeImage = fileName;
                    qrCode.MenuUrl = menuUrl;
                }
                qrCode.TableID = model.TableID;
                qrCode.Status = model.Status;

                await _qrCodeService.UpdateQRCodeAsync(qrCode);
                _logger.LogInformation("QR code updated successfully: {QRCodeID}", qrCode.QRCodeID);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating QR code");
                return Json(new { success = false, message = "Error updating QR code" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> DeleteQRCode(int id)
        {
            try
            {
                var qrCode = await _qrCodeService.GetQRCodeByIdAsync(id);
                if (qrCode == null)
                    return Json(new { success = false, message = "QR code not found" });

                if (!string.IsNullOrEmpty(qrCode.QRCodeImage))
                {
                    var imagePath = Path.Combine(_environment.WebRootPath, "images", "QRCodes", qrCode.QRCodeImage);
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);
                }

                await _qrCodeService.DeleteQRCodeAsync(id);
                _logger.LogInformation("QR code deleted successfully: {QRCodeID}", id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting QR code");
                return Json(new { success = false, message = "Error deleting QR code" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TrackQRCodeAccess(int qrCodeId)
        {
            try
            {
                var qrCode = await _qrCodeService.GetQRCodeByIdAsync(qrCodeId);
                if (qrCode == null || qrCode.Status == 0)
                    return NotFound();

                qrCode.AccessCount++;
                await _qrCodeService.UpdateQRCodeAsync(qrCode);
                return Redirect(qrCode.MenuUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking QR code access");
                return StatusCode(500);
            }
        }
    }

    public class QRCodeCreateModel
    {
        public int TableID { get; set; }
    }

    public class QRCodeUpdateModel
    {
        public int QRCodeID { get; set; }
        public int TableID { get; set; }
        public byte Status { get; set; }
    }
}