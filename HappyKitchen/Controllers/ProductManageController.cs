using HappyKitchen.Attributes;
using HappyKitchen.Models;
using HappyKitchen.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HappyKitchen.Controllers
{
    [AuthorizeAccess]
    public class ProductManageController : Controller
    {
        private readonly IMenuItemService _menuItemService;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<ProductManageController> _logger;
        private readonly IWebHostEnvironment _environment;

        public ProductManageController(
            IMenuItemService menuItemService,
            ICategoryService categoryService,
            ILogger<ProductManageController> logger,
            IWebHostEnvironment environment)
        {
            _menuItemService = menuItemService;
            _categoryService = categoryService;
            _logger = logger;
            _environment = environment;
        }
        
        [HttpGet]
        [AuthorizeAccess("MENU_MANAGE", "view")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [AuthorizeAccess("MENU_MANAGE", "view")]
        public async Task<IActionResult> GetMenuItems(
                int page = 1,
                int pageSize = 8,
                string searchTerm = "",
                string status = "all",
                int categoryId = 0,
                string sortBy = "name_asc")
            {
            _logger.LogDebug("[API] GetMenuItems: page={Page}, size={Size}, search={Search}, status={Status}, category={Category}, sort={Sort}", 
                page, pageSize, searchTerm, status, categoryId, sortBy);
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var menuItems = await _menuItemService.GetAllMenuItemsAsync();
                _logger.LogDebug("Tìm thấy {Count} món ăn", menuItems.Count);

                // Áp dụng các bộ lọc
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    menuItems = menuItems.Where(m =>
                        m.Name.ToLower().Contains(searchTerm) ||
                        m.Description?.ToLower().Contains(searchTerm) == true
                    ).ToList();
                }

                if (status != "all")
                {
                    byte statusValue = byte.Parse(status);
                    menuItems = menuItems.Where(m => m.Status == statusValue).ToList();
                }

                if (categoryId > 0)
                {
                    menuItems = menuItems.Where(m => m.CategoryID == categoryId).ToList();
                }

                // Sắp xếp
                menuItems = sortBy switch
                {
                    "name_asc" => menuItems.OrderBy(m => m.Name).ToList(),
                    "name_desc" => menuItems.OrderByDescending(m => m.Name).ToList(),
                    "price_asc" => menuItems.OrderBy(m => m.Price).ToList(),
                    "price_desc" => menuItems.OrderByDescending(m => m.Price).ToList(),
                    _ => menuItems.OrderBy(m => m.Name).ToList()
                };

                // Phân trang
                int totalItems = menuItems.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                
                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
                
                var pagedMenuItems = menuItems
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                stopwatch.Stop();
                _logger.LogDebug("GetMenuItems hoàn thành trong {ElapsedMs}ms, trả về {Count}/{Total} món ăn", 
                    stopwatch.ElapsedMilliseconds, pagedMenuItems.Count, totalItems);

                return Json(new
                {
                    success = true,
                    data = pagedMenuItems.Select(m => new
                    {
                        m.MenuItemID,
                        m.Name,
                        m.MenuItemImage,
                        CategoryName = m.Category?.CategoryName,
                        m.CategoryID,
                        m.Price,
                        m.Description,
                        m.Status,
                        Attributes = m.Attributes.Select(a => new
                        {
                            a.AttributeID,
                            a.AttributeName,
                            a.AttributeValue
                        }),
                        m.AverageRating,
                        m.RatingCount,
                        m.TotalComments
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
                _logger.LogError(ex, "Lỗi trong GetMenuItems ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi lấy danh sách món ăn" });
            }
        }

        [HttpGet]
        [AuthorizeAccess("MENU_MANAGE", "view")]
        public async Task<IActionResult> GetCategories()
        {
            _logger.LogDebug("[API] GetCategories");
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                
                stopwatch.Stop();
                _logger.LogDebug("GetCategories hoàn thành trong {ElapsedMs}ms, trả về {Count} danh mục", 
                    stopwatch.ElapsedMilliseconds, categories.Count);
                
                return Json(new
                {
                    success = true,
                    data = categories.Select(c => new
                    {
                        c.CategoryID,
                        c.CategoryName,
                        ProductCount = c.MenuItems?.Count ?? 0
                    })
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Lỗi trong GetCategories ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi lấy danh sách danh mục" });
            }
        }

        [HttpPost]
        [AuthorizeAccess("MENU_MANAGE", "add")]
        public async Task<IActionResult> CreateMenuItem([FromForm] MenuItemCreateModel model, IFormFile image)
        {
            _logger.LogDebug("[API] CreateMenuItem: Tên={Name}, Giá={Price}, DanhMục={CategoryID}, Trạng thái={Status}, SốThuộcTính={AttrCount}", 
                model.MenuItem?.Name, model.MenuItem?.Price, model.MenuItem?.CategoryID, model.MenuItem?.Status, model.Attributes?.Count ?? 0);
            
            if (image != null)
            {
                _logger.LogDebug("Ảnh: Tên={FileName}, Kích thước={Size}KB, Loại={ContentType}", 
                    image.FileName, image.Length / 1024, image.ContentType);
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Validation
                if (model.MenuItem == null)
                {
                    return Json(new { success = false, message = "Dữ liệu sản phẩm không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(model.MenuItem.Name) || model.MenuItem.Name.Length > 100)
                {
                    return Json(new { success = false, message = "Tên món ăn bắt buộc và phải <= 100 ký tự" });
                }

                if (model.MenuItem.Price < 0)
                {
                    return Json(new { success = false, message = "Giá phải không âm" });
                }

                if (model.MenuItem.CategoryID <= 0)
                {
                    return Json(new { success = false, message = "Danh mục hợp lệ là bắt buộc" });
                }

                if (!string.IsNullOrEmpty(model.MenuItem.Description) && model.MenuItem.Description.Length > 255)
                {
                    return Json(new { success = false, message = "Mô tả phải <= 255 ký tự" });
                }

                var menuItem = new MenuItem
                {
                    Name = model.MenuItem.Name,
                    Price = model.MenuItem.Price,
                    CategoryID = model.MenuItem.CategoryID,
                    Description = model.MenuItem.Description,
                    Status = model.MenuItem.Status,
                    Attributes = model.Attributes?.Select(a => new MenuItemAttribute
                    {
                        AttributeName = a.AttributeName,
                        AttributeValue = a.AttributeValue
                    }).ToList() ?? new List<MenuItemAttribute>()
                };

                if (image != null && image.Length > 0)
                {
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                    var filePath = Path.Combine(_environment.WebRootPath, "images", "MenuItem", fileName);
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    menuItem.MenuItemImage = fileName;
                    _logger.LogDebug("Đã lưu ảnh: {FileName}", fileName);
                }
                else
                {
                    menuItem.MenuItemImage = "default.jpg";
                }

                await _menuItemService.CreateMenuItemAsync(menuItem);
                
                stopwatch.Stop();
                _logger.LogInformation("Tạo món ăn thành công trong {ElapsedMs}ms: {Name} (ID: {ID})", 
                    stopwatch.ElapsedMilliseconds, menuItem.Name, menuItem.MenuItemID);
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Lỗi trong CreateMenuItem ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi tạo món ăn" });
            }
        }

        [HttpPost]
        [AuthorizeAccess("MENU_MANAGE", "edit")]
        public async Task<IActionResult> UpdateMenuItem([FromForm] MenuItemUpdateModel model, IFormFile image, [FromForm] bool isDeleted = false)
        {
            if (model?.MenuItem == null)
            {
                _logger.LogError("[API] UpdateMenuItem: Model là null");
                return Json(new { success = false, message = "Dữ liệu món ăn bắt buộc" });
            }
            
            var menuItem = model.MenuItem;
            _logger.LogDebug("[API] UpdateMenuItem: ID={ID}, Tên={Name}, Giá={Price}, DanhMục={CategoryID}, Trạng thái={Status}, SốThuộcTính={AttrCount}, IsDeleted={IsDeleted}", 
                menuItem.MenuItemID, menuItem.Name, menuItem.Price, menuItem.CategoryID, menuItem.Status, model.Attributes?.Count ?? 0, isDeleted);
            
            if (image != null)
            {
                _logger.LogDebug("Ảnh mới: Tên={FileName}, Kích thước={Size}KB, Loại={ContentType}", 
                    image.FileName, image.Length / 1024, image.ContentType);
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(menuItem.Name) || menuItem.Name.Length > 100)
                {
                    return Json(new { success = false, message = "Tên món ăn bắt buộc và phải <= 100 ký tự" });
                }

                if (menuItem.Price < 0)
                {
                    return Json(new { success = false, message = "Giá phải không âm" });
                }

                if (menuItem.CategoryID <= 0)
                {
                    return Json(new { success = false, message = "Danh mục hợp lệ là bắt buộc" });
                }

                if (!string.IsNullOrEmpty(menuItem.Description) && menuItem.Description.Length > 255)
                {
                    return Json(new { success = false, message = "Mô tả phải <= 255 ký tự" });
                }

                var existingMenuItem = await _menuItemService.GetMenuItemByIdAsync(menuItem.MenuItemID);
                if (existingMenuItem == null)
                {
                    _logger.LogWarning("Không tìm thấy món ăn với ID: {ID}", menuItem.MenuItemID);
                    return Json(new { success = false, message = "Không tìm thấy món ăn" });
                }
                
                _logger.LogDebug("Cập nhật món ăn: {OldName} -> {NewName}", existingMenuItem.Name, menuItem.Name);

                // Update basic fields
                existingMenuItem.Name = menuItem.Name;
                existingMenuItem.Price = menuItem.Price;
                existingMenuItem.CategoryID = menuItem.CategoryID;
                existingMenuItem.Description = menuItem.Description;
                existingMenuItem.Status = menuItem.Status;

                // Handle image update
                if (image != null && image.Length > 0)
                {
                    // Case 1: New image provided
                    // Delete old image if it exists and is not default
                    if (!string.IsNullOrEmpty(existingMenuItem.MenuItemImage) && existingMenuItem.MenuItemImage != "default.jpg")
                    {
                        var oldImagePath = Path.Combine(_environment.WebRootPath, "images", "MenuItem", existingMenuItem.MenuItemImage);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                            _logger.LogDebug("Đã xóa ảnh cũ: {FileName}", existingMenuItem.MenuItemImage);
                        }
                    }

                    // Save new image
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                    var filePath = Path.Combine(_environment.WebRootPath, "images", "MenuItem", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    existingMenuItem.MenuItemImage = fileName;
                    _logger.LogDebug("Đã lưu ảnh mới: {FileName}", fileName);
                }
                else if (isDeleted)
                {
                    // Case 3: Image deletion requested
                    if (!string.IsNullOrEmpty(existingMenuItem.MenuItemImage) && existingMenuItem.MenuItemImage != "default.jpg")
                    {
                        var oldImagePath = Path.Combine(_environment.WebRootPath, "images", "MenuItem", existingMenuItem.MenuItemImage);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                            _logger.LogDebug("Đã xóa ảnh: {FileName}", existingMenuItem.MenuItemImage);
                        }
                    }
                    existingMenuItem.MenuItemImage = ""; // Clear image in database
                    _logger.LogDebug("Đã xóa tham chiếu ảnh trong cơ sở dữ liệu");
                }
                // Case 2: No new image and no deletion (image == null && !isDeleted)
                // Do nothing, keep existingMenuItem.MenuItemImage unchanged

                // Update attributes
                existingMenuItem.Attributes.Clear();
                if (model.Attributes != null)
                {
                    _logger.LogDebug("Cập nhật {Count} thuộc tính", model.Attributes.Count);
                    existingMenuItem.Attributes = model.Attributes.Select(a => new MenuItemAttribute
                    {
                        AttributeName = a.AttributeName,
                        AttributeValue = a.AttributeValue
                    }).ToList();
                }

                await _menuItemService.UpdateMenuItemAsync(existingMenuItem);
                
                stopwatch.Stop();
                _logger.LogInformation("Cập nhật món ăn thành công trong {ElapsedMs}ms: {Name} (ID: {ID})", 
                    stopwatch.ElapsedMilliseconds, existingMenuItem.Name, existingMenuItem.MenuItemID);
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Lỗi trong UpdateMenuItem ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi cập nhật món ăn" });
            }
        }

        [HttpPost]
        [AuthorizeAccess("MENU_MANAGE", "delete")]
        public async Task<JsonResult> DeleteMenuItem(int id)
        {
            _logger.LogDebug("[API] DeleteMenuItem: ID={ID}", id);
            try
            {
                var menuItem = await _menuItemService.GetMenuItemByIdAsync(id);
                if (menuItem == null)
                    return Json(new { success = false, message = "Không tìm thấy món ăn" });

                if (!string.IsNullOrEmpty(menuItem.MenuItemImage) && menuItem.MenuItemImage != "default.jpg")
                {
                    var imagePath = Path.Combine(_environment.WebRootPath, "images", "MenuItem", menuItem.MenuItemImage);
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);
                }

                await _menuItemService.DeleteMenuItemAsync(id);
                _logger.LogInformation("Món ăn được xóa thành công: {MenuItemId}", id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa món ăn");
                return Json(new { success = false, message = "Lỗi khi xóa món ăn" });
            }
        }

        [HttpPost]
        [AuthorizeAccess("MENU_MANAGE", "add")]
        public async Task<JsonResult> CreateCategory([FromBody] Category category)
        {
            _logger.LogDebug("[API] CreateCategory: Tên={Name}", category.CategoryName);
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                if (string.IsNullOrWhiteSpace(category.CategoryName) || category.CategoryName.Length > 100)
                {
                    return Json(new { success = false, message = "Tên danh mục bắt buộc và phải <= 100 ký tự" });
                }

                var existingCategory = await _categoryService.GetCategoryByNameAsync(category.CategoryName);
                if (existingCategory != null)
                {
                    _logger.LogWarning("Danh mục đã tồn tại: {Name}", category.CategoryName);
                    return Json(new { success = false, message = "Danh mục đã tồn tại" });
                }

                await _categoryService.CreateCategoryAsync(category);
                
                stopwatch.Stop();
                _logger.LogInformation("Tạo danh mục thành công trong {ElapsedMs}ms: {Name} (ID: {ID})", 
                    stopwatch.ElapsedMilliseconds, category.CategoryName, category.CategoryID);
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Lỗi trong CreateCategory ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi tạo danh mục" });
            }
        }
    }

    public class MenuItemCreateModel
    {
        public MenuItem MenuItem { get; set; }
        public List<MenuItemAttributeModel> Attributes { get; set; }
    }

    public class MenuItemUpdateModel
    {
        public MenuItem MenuItem { get; set; }
        public List<MenuItemAttributeModel> Attributes { get; set; }
    }

    public class MenuItemAttributeModel
    {
        public string AttributeName { get; set; }
        public string AttributeValue { get; set; }
    }
}