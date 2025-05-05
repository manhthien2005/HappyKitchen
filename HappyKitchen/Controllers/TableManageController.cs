using HappyKitchen.Models;
using HappyKitchen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HappyKitchen.Controllers
{
    public class TableManageController : Controller
    {
        private readonly IAreaTableService _areaTableService;
        private readonly ILogger<TableManageController> _logger;

        public TableManageController(IAreaTableService areaTableService, ILogger<TableManageController> logger)
        {
            _areaTableService = areaTableService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Area Endpoints
        [HttpGet]
        public async Task<IActionResult> GetAreas(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            _logger.LogDebug("[API] GetAreas: search={Search}, page={Page}, size={Size}", searchTerm, page, pageSize);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var areas = await _areaTableService.GetAllAreasAsync(searchTerm);

                // Pagination
                int totalItems = areas.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                page = Math.Max(1, Math.Min(page, totalPages));

                var pagedAreas = areas
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                stopwatch.Stop();
                _logger.LogDebug("GetAreas completed in {ElapsedMs}ms, returned {Count}/{Total} areas",
                    stopwatch.ElapsedMilliseconds, pagedAreas.Count, totalItems);

                return Json(new
                {
                    success = true,
                    data = pagedAreas.Select(a => new
                    {
                        a.AreaID,
                        a.AreaName,
                        a.Description,
                        TableCount = a.Tables?.Count ?? 0
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
                stopwatch.Stop();
                _logger.LogError(ex, "Error in GetAreas ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Error retrieving areas" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateArea([FromBody] Area model)
        {
            _logger.LogDebug("[API] CreateArea: AreaName={AreaName}", model.AreaName);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrWhiteSpace(model.AreaName))
                {
                    return Json(new { success = false, message = "Area name is required" });
                }

                if ((await _areaTableService.GetAllAreasAsync(model.AreaName)).Any(a => a.AreaName.ToLower() == model.AreaName.ToLower()))
                {
                    return Json(new { success = false, message = "Area name already exists" });
                }

                var area = new Area
                {
                    AreaName = model.AreaName.Trim(),
                    Description = model.Description?.Trim()
                };

                await _areaTableService.CreateAreaAsync(area);

                stopwatch.Stop();
                _logger.LogInformation("Area created in {ElapsedMs}ms: AreaID={AreaID}", stopwatch.ElapsedMilliseconds, area.AreaID);

                return Json(new { success = true, areaID = area.AreaID });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in CreateArea ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Error creating area" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateArea([FromBody] Area model)
        {
            _logger.LogDebug("[API] UpdateArea: AreaID={AreaID}, AreaName={AreaName}", model.AreaID, model.AreaName);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var area = await _areaTableService.GetAreaByIdAsync(model.AreaID);

                if (string.IsNullOrWhiteSpace(model.AreaName))
                {
                    return Json(new { success = false, message = "Area name is required" });
                }

                if ((await _areaTableService.GetAllAreasAsync(model.AreaName))
                    .Any(a => a.AreaName.ToLower() == model.AreaName.ToLower() && a.AreaID != model.AreaID))
                {
                    return Json(new { success = false, message = "Area name already exists" });
                }

                area.AreaName = model.AreaName.Trim();
                area.Description = model.Description?.Trim();

                await _areaTableService.UpdateAreaAsync(area);

                stopwatch.Stop();
                _logger.LogInformation("Area updated in {ElapsedMs}ms: AreaID={AreaID}", stopwatch.ElapsedMilliseconds, area.AreaID);

                return Json(new { success = true });
            }
            catch (KeyNotFoundException)
            {
                stopwatch.Stop();
                _logger.LogWarning("Area not found in UpdateArea ({ElapsedMs}ms): AreaID={AreaID}", stopwatch.ElapsedMilliseconds, model.AreaID);
                return Json(new { success = false, message = "Area not found" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in UpdateArea ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Error updating area" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteArea(int id)
        {
            _logger.LogDebug("[API] DeleteArea: AreaID={AreaID}", id);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _areaTableService.DeleteAreaAsync(id);

                stopwatch.Stop();
                _logger.LogInformation("Area deleted in {ElapsedMs}ms: AreaID={AreaID}", stopwatch.ElapsedMilliseconds, id);

                return Json(new { success = true });
            }
            catch (KeyNotFoundException)
            {
                stopwatch.Stop();
                _logger.LogWarning("Area not found in DeleteArea ({ElapsedMs}ms): AreaID={AreaID}", stopwatch.ElapsedMilliseconds, id);
                return Json(new { success = false, message = "Area not found" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in DeleteArea ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Error deleting area" });
            }
        }

        // Table Endpoints
        [HttpGet]
        public async Task<IActionResult> GetTables(string searchTerm = "", int areaId = 0, string status = "all", int page = 1, int pageSize = 10)
        {
            _logger.LogDebug("[API] GetTables: search={Search}, area={AreaID}, status={Status}, page={Page}, size={Size}",
                searchTerm, areaId, status, page, pageSize);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var tables = await _areaTableService.GetAllTablesAsync(searchTerm, areaId, status);

                // Pagination
                int totalItems = tables.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                page = Math.Max(1, Math.Min(page, totalPages));

                var pagedTables = tables
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                stopwatch.Stop();
                _logger.LogDebug("GetTables completed in {ElapsedMs}ms, returned {Count}/{Total} tables",
                    stopwatch.ElapsedMilliseconds, pagedTables.Count, totalItems);

                return Json(new
                {
                    success = true,
                    data = pagedTables.Select(t => new
                    {
                        t.TableID,
                        t.TableName,
                        t.AreaID,
                        AreaName = t.Area?.AreaName ?? "Không có khu vực",
                        t.Capacity,
                        t.Status,
                        StatusText = t.Status switch { 0 => "Trống", 1 => "Đã đặt trước", 2 => "Đang sử dụng", _ => "Không xác định" }
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
                stopwatch.Stop();
                _logger.LogError(ex, "Error in GetTables ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Error retrieving tables" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateTable([FromBody] Table model)
        {
            _logger.LogDebug("[API] CreateTable: TableName={TableName}, AreaID={AreaID}", model.TableName, model.AreaID);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrWhiteSpace(model.TableName))
                {
                    return Json(new { success = false, message = "Table name is required" });
                }

                if (model.Capacity <= 0)
                {
                    return Json(new { success = false, message = "Capacity must be positive" });
                }

                if (model.Status is < 0 or > 2)
                {
                    return Json(new { success = false, message = "Invalid status" });
                }


                var table = new Table
                {
                    TableName = model.TableName.Trim(),
                    AreaID = model.AreaID,
                    Capacity = model.Capacity,
                    Status = model.Status
                };

                await _areaTableService.CreateTableAsync(table);

                stopwatch.Stop();
                _logger.LogInformation("Table created in {ElapsedMs}ms: TableID={TableID}", stopwatch.ElapsedMilliseconds, table.TableID);

                return Json(new { success = true, tableID = table.TableID });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in CreateTable ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Error creating table" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateTable([FromBody] Table model)
        {
            _logger.LogDebug("[API] UpdateTable: TableID={TableID}, TableName={TableName}, AreaID={AreaID}",
                model.TableID, model.TableName, model.AreaID);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var table = await _areaTableService.GetTableByIdAsync(model.TableID);

                if (string.IsNullOrWhiteSpace(model.TableName))
                {
                    return Json(new { success = false, message = "Table name is required" });
                }

                if (model.Capacity <= 0)
                {
                    return Json(new { success = false, message = "Capacity must be positive" });
                }

                if (model.Status is < 0 or > 2)
                {
                    return Json(new { success = false, message = "Invalid status" });
                }

                table.TableName = model.TableName.Trim();
                table.AreaID = model.AreaID;
                table.Capacity = model.Capacity;
                table.Status = model.Status;

                await _areaTableService.UpdateTableAsync(table);

                stopwatch.Stop();
                _logger.LogInformation("Table updated in {ElapsedMs}ms: TableID={TableID}", stopwatch.ElapsedMilliseconds, table.TableID);

                return Json(new { success = true });
            }
            catch (KeyNotFoundException)
            {
                stopwatch.Stop();
                _logger.LogWarning("Table not found in UpdateTable ({ElapsedMs}ms): TableID={TableID}", stopwatch.ElapsedMilliseconds, model.TableID);
                return Json(new { success = false, message = "Table not found" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in UpdateTable ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Error updating table" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteTable(int id)
        {
            _logger.LogDebug("[API] DeleteTable: TableID={TableID}", id);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _areaTableService.DeleteTableAsync(id);

                stopwatch.Stop();
                _logger.LogInformation("Table deleted in {ElapsedMs}ms: TableID={TableID}", stopwatch.ElapsedMilliseconds, id);

                return Json(new { success = true });
            }
            catch (KeyNotFoundException)
            {
                stopwatch.Stop();
                _logger.LogWarning("Table not found in DeleteTable ({ElapsedMs}ms): TableID={TableID}", stopwatch.ElapsedMilliseconds, id);
                return Json(new { success = false, message = "Table not found" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in DeleteTable ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Error deleting table" });
            }
        }
    }
}