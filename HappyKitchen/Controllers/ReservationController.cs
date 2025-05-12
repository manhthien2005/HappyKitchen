using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HappyKitchen.Data;
using HappyKitchen.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HappyKitchen.Controllers
{
    public class ReservationController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public ReservationController(ApplicationDbContext context, IConfiguration configuration)
        {

            _context = context;
            _configuration = configuration;
        }

        public IActionResult Menuing()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Menuing([FromBody] DishCheckingViewModel cartInfoJson)
        {
            // Kiểm tra dish hoặc ReservationInformation có null không
            if (cartInfoJson == null)
            {

                return BadRequest("Dữ liệu đặt bàn không hợp lệ.");
            }


            // Lấy danh sách danh mục món ăn
            var categories = await _context.Categories
                .Include(c => c.MenuItems)
                .Where(c => c.MenuItems.Any(m => m.Status == 1))
                .ToListAsync();

            // Kiểm tra bàn được chọn
            var selectTable = await _context.Tables
                .Where(c => c.TableID == cartInfoJson.ReservationInformation.TableID)
                .FirstOrDefaultAsync();

            if (selectTable == null)
            {
                return BadRequest("Bàn được chọn không tồn tại.");
            }
            cartInfoJson.ReservationInformation.Table = selectTable;
            // Kiểm tra xem đặt bàn đã tồn tại chưa
            var existingReservation = await _context.Reservations
                .FirstOrDefaultAsync(r =>
                    r.CustomerPhone == cartInfoJson.ReservationInformation.CustomerPhone &&
                    r.ReservationTime == cartInfoJson.ReservationInformation.ReservationTime &&
                    r.TableID == cartInfoJson.ReservationInformation.TableID);



            // Nếu chưa tồn tại, thêm mới vào database
            if (existingReservation == null)
            {
                _context.Reservations.Add(cartInfoJson.ReservationInformation); 
                await _context.SaveChangesAsync();
            }
            else
            {
                // Có thể ghi log hoặc xử lý thêm nếu cần
                Console.WriteLine("Đặt bàn đã tồn tại, không thêm mới.");
            }

            // Tạo view model để trả về
            var menuView = new MenuViewModel
            {
                Cart = cartInfoJson,
                Categories = categories
            };

            return View(menuView);
        }


        [HttpPost]
        public async Task<IActionResult> DishChecking([FromBody] DishCheckingViewModel viewModel)
        {
            if (viewModel == null || viewModel.CartItems == null)
            {
                return BadRequest("Dữ liệu JSON không hợp lệ.");
            }

            foreach (var item in viewModel.CartItems)
            {
                item.MenuItem = _context.MenuItems.FirstOrDefault(m => m.MenuItemID == item.MenuItemID);
                Console.WriteLine($"Item processed - MenuItemID: {item.MenuItemID}, MenuItem: {item.MenuItem?.ToString() ?? "null"}");
            }

            string jsonString = JsonConvert.SerializeObject(viewModel);
            HttpContext.Session.SetString("CartSession", jsonString);


            string sessionData = HttpContext.Session.GetString("CartSession");


            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(byte payment)
        {
            
            int? userId = HttpContext.Session.GetInt32("UserID");
            // Lấy session
            string cartJson = HttpContext.Session.GetString("CartSession");



            // Khởi tạo cartItems mặc định
            DishCheckingViewModel cartItems = new DishCheckingViewModel
            {
                ReservationInformation = null,
                CartItems = new List<CartItem>()
            };

            // Deserialize cartJson nếu tồn tại
            if (!string.IsNullOrEmpty(cartJson))
            {
                try
                {
                    cartItems = JsonConvert.DeserializeObject<DishCheckingViewModel>(cartJson);
                    //Console.WriteLine($"DEBUG: Deserialized cartItems: {(cartItems != null ? $"CartItems count: {cartItems.CartItems?.Count ?? 0}" : "null")}");

                    // Kiểm tra và khởi tạo CartItems nếu null
                    if (cartItems != null && cartItems.CartItems == null)
                    {
                        //Console.WriteLine("DEBUG: CartItems is null, initializing to empty list");
                        cartItems.CartItems = new List<CartItem>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Deserialize error: {ex.Message}");

                    cartItems = new DishCheckingViewModel
                    {
                        ReservationInformation = null,
                        CartItems = new List<CartItem>()
                    };
                }
            }
            else
            {
                Console.WriteLine("DEBUG: cartJson is null or empty, using default empty cart");
            }

            try
            {

                // Tạo một Order mới
                var order = new Order
                {
                    CustomerID = userId, // Cần lấy từ thông tin người dùng nếu có (ví dụ: từ session hoặc đăng nhập)
                    EmployeeID = 1, 
                    TableID = cartItems.ReservationInformation.TableID, // Giả định TableID, bạn cần lấy từ form hoặc logic khác
                    OrderTime = cartItems.ReservationInformation.CreatedTime,
                    Status = 1, // 1 = Pending Confirmation
                    PaymentMethod = payment // Giả định phương thức thanh toán, có thể lấy từ form
                };
                

                // Thêm Order vào DbContext
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                cartItems.ReservationInformation.OrderID = order.OrderID; // Gán OrderID cho ReservationInformation 
                _context.Reservations.Update(cartItems.ReservationInformation);
                await _context.SaveChangesAsync();
                // Tạo OrderDetails từ CartItems
                foreach (var cartItem in cartItems.CartItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderID = order.OrderID, // Gán OrderID từ Order vừa tạo
                        MenuItemID = cartItem.MenuItemID,
                        Quantity = cartItem.Quantity
                    };
                    _context.OrderDetails.Add(orderDetail);
                }

                // Lưu các OrderDetail vào cơ sở dữ liệu
                await _context.SaveChangesAsync();

                // Xóa giỏ hàng sau khi tạo đơn hàng thành công
                HttpContext.Session.Remove("CartSession");

                if (payment == 2)
                {

                    var VNPayRequest = new VNPayRequest
                    {
                        OrderId = order.OrderID.ToString(),
                        Amount =(int)Math.Round(cartItems.TotalPrice + (cartItems.TotalPrice * 0.2m)),
                        OrderDescription = "T"+order.OrderID.ToString(),
                    };


                    // Lưu VNPayRequest vào TempData dưới dạng JSON
                    TempData["VNPayRequest"] = JsonConvert.SerializeObject(VNPayRequest);

                    return RedirectToAction("CreatePayment");
                }
                else 
                {
                    cartItems.ReservationInformation.Status = 2;
                    _context.Reservations.Update(cartItems.ReservationInformation);
                    order.Status = 2; // Giả định trạng thái đã thanh toán
                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();
                    ViewBag.Message = $"Đơn hàng đã được tạo thành công với phương thức {payment}.";
                    if (payment == 1) return RedirectToAction("CardReturn", new { id = order.OrderID, price = (int)cartItems.TotalPrice + (cartItems.TotalPrice * 0.2m) });
                    else
                    {
                        order.Status = 3; // Giả định trạng thái đã thanh toán
                        _context.Orders.Update(order);
                        await _context.SaveChangesAsync(); 
                        return RedirectToAction("CashReturn", new { id = order.OrderID, price = (int)cartItems.TotalPrice + (cartItems.TotalPrice * 0.2m) });
                    }
                    



                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error creating order: {ex.Message}");
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi tạo đơn hàng. Vui lòng thử lại.";
                return View();
            }

        }
        //THANH TOÁN


        public IActionResult CashReturn(int id, int price)
        {
            ViewBag.Message = "Thanh toán thành công!";
            ViewBag.TransactionId = id;
            ViewBag.Amount = price;
            ViewBag.ShowCountdown = true; // Thêm biến để kích hoạt đếm ngược
            return View();
        }

        public IActionResult CardReturn(int id, int price)
        {
            ViewBag.Message = "Thanh toán thành công!";
            ViewBag.TransactionId = id;
            ViewBag.Amount = price;
            ViewBag.ShowCountdown = true; // Thêm biến để kích hoạt đếm ngược
            return View();
        }

        [HttpGet]
        public IActionResult CreatePayment()
        {
            if (TempData["VNPayRequest"] is string requestJson && !string.IsNullOrEmpty(requestJson))
            {
                try
                {
                    var request = JsonConvert.DeserializeObject<VNPayRequest>(requestJson);
                    if (request == null || request.Amount <= 0)
                    {
                        Console.WriteLine("DEBUG: Invalid VNPayRequest data - Amount: {0}", request?.Amount ?? 0);
                        return BadRequest("Dữ liệu thanh toán không hợp lệ.");
                    }

                    Console.WriteLine("DEBUG: VNPayRequest data - Amount: {0}", request?.Amount ?? 0);

                    // Lấy thông tin cấu hình từ appsettings.json
                    string tmnCode = _configuration["VNPay:TmnCode"];
                    string hashSecret = _configuration["VNPay:HashSecret"];
                    string vnpUrl = _configuration["VNPay:BaseUrl"];
                    string returnUrl = _configuration["VNPay:ReturnUrl"];

                    // Tạo URL thanh toán
                    string vnp_TxnRef = request.OrderId;
                    string vnp_Amount = (request.Amount * 100).ToString(CultureInfo.InvariantCulture);
                    string vnp_OrderInfo = request?.OrderDescription ?? "Thanh toan don hang";
                    string vnp_IpAddr = request?.ClientIp ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                    string vnp_CreateDate = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"); // UTC+7
                    string vnp_Locale = "vn";
                    string vnp_CurrCode = "VND";

                    // Tạo danh sách tham số
                    var vnp_Params = new SortedDictionary<string, string>
                    {
                        { "vnp_Version", "2.1.0" },
                        { "vnp_Command", "pay" },
                        { "vnp_TmnCode", tmnCode },
                        { "vnp_Amount", vnp_Amount },
                        { "vnp_CreateDate", vnp_CreateDate },
                        { "vnp_CurrCode", vnp_CurrCode },
                        { "vnp_IpAddr", vnp_IpAddr },
                        { "vnp_Locale", vnp_Locale },
                        { "vnp_OrderInfo", vnp_OrderInfo },
                        { "vnp_OrderType", "billpayment" },
                        { "vnp_ReturnUrl", returnUrl },
                        { "vnp_TxnRef", vnp_TxnRef }
                    };
                    Console.WriteLine("DEBUG: vnp_Params before signing:");
                    foreach (var param in vnp_Params)
                    {
                        Console.WriteLine($"  {param.Key}: {param.Value}");
                    }
                    // Tạo chữ ký bảo mật
                    string signData = string.Join("&", vnp_Params.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                    Console.WriteLine("signData: " + signData); // Debug
                    string vnp_SecureHash = HmacSHA512(hashSecret, signData);
                    Console.WriteLine("vnp_SecureHash: " + vnp_SecureHash); // Debug
                    vnp_Params["vnp_SecureHash"] = vnp_SecureHash;

                    // Tạo URL thanh toán
                    string paymentUrl = vnpUrl + "?" + string.Join("&", vnp_Params.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                    Console.WriteLine("paymentUrl: " + paymentUrl); // Debug

                    return Redirect(paymentUrl);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine("DEBUG: Deserialize error: {0}", ex.Message);
                    return BadRequest("Lỗi phân tích dữ liệu thanh toán.");
                }
                

            }
            else
            {
                Console.WriteLine("DEBUG: No VNPayRequest data in TempData");
                return BadRequest("Không có thông tin thanh toán.");
            }

        }

        [HttpGet]
        public async Task<IActionResult> VNPayReturn()
        {


            // Lấy thông tin từ VNPay trả về
            string hashSecret = _configuration["VNPay:HashSecret"];
            var vnp_Params = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());

            string vnp_SecureHash = vnp_Params["vnp_SecureHash"];
            vnp_Params.Remove("vnp_SecureHash");



            // Tạo lại chữ ký để kiểm tra
            var signData = string.Join("&", vnp_Params.OrderBy(k => k.Key).Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            string checkSum = HmacSHA512(hashSecret, signData);

            // Kiểm tra tính hợp lệ của giao dịch
            if (checkSum.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase))
            {
                if (vnp_Params["vnp_ResponseCode"] == "00")
                {
                    ViewBag.Message = "Thanh toán thành công!";
                    ViewBag.TransactionId = vnp_Params["vnp_TxnRef"];
                    ViewBag.Amount = decimal.Parse(vnp_Params["vnp_Amount"]) / 100;
                    ViewBag.ShowCountdown = true; // Thêm biến để kích hoạt đếm ngược
                    var order = await _context.Orders
                    .Where(r => r.OrderID.ToString() == vnp_Params["vnp_TxnRef"])
                    .FirstOrDefaultAsync();
                    order.Status = 2; // Cập nhật trạng thái thành "đã thanh toán"
                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    ViewBag.Message = "Thanh toán thất bại!";
                }
            }
            else
            {
                ViewBag.Message = "Chữ ký không hợp lệ!";
            }

            return View();
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }


    }

}
