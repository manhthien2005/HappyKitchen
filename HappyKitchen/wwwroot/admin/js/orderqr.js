document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        tableId: null,
        tableName: null,
        menuItems: [],
        categories: [],
        orderItems: [],
        searchTerm: "",
        categoryFilter: 0,
        currentPage: 1,
        totalPages: 1,
        userInfo: null
    };

    // API Endpoints
    const API = {
        validateTable: (tableId) => `/OrderQR/ValidateTable?tableId=${tableId}`,
        menuItems: (page, pageSize, searchTerm, categoryId) =>
            `/OrderQR/GetMenuItems?page=${page}&pageSize=${pageSize}` +
            (searchTerm ? `&searchTerm=${encodeURIComponent(searchTerm)}` : '') +
            (categoryId > 0 ? `&categoryId=${categoryId}` : ''),
        categories: '/OrderQR/GetCategories',
        createOrder: '/OrderQR/CreateOrder',
        checkUserLogin: '/User/CheckLoginStatus'
    };

    // DOM Elements
    const DOM = {
        tableName: document.getElementById("table-name"),
        customerName: document.getElementById("customer-name"),
        customerPhone: document.getElementById("customer-phone"),
        menuItemSearchInput: document.getElementById("menuItemSearchInput"),
        categoryTabs: document.getElementById("category-tabs"),
        menuItemsContainer: document.getElementById("menu-items-container"),
        menuPagination: document.getElementById("menu-pagination"),
        menuPaginationPrev: document.getElementById("menu-pagination-prev"),
        menuPaginationItems: document.getElementById("menu-pagination-items"),
        menuPaginationNext: document.getElementById("menu-pagination-next"),
        selectedItemsContainer: document.getElementById("selected-items-container"),
        totalPrice: document.getElementById("total-price"),
        submitOrderBtn: document.getElementById("submit-order-btn"),
        scrollTopButton: document.getElementById("scroll-top-button"),
        menuForm: document.getElementById("menu-form"),
        loadingOverlay: document.getElementById("loading-overlay"),
        toastContainer: document.getElementById("toast-container"),
        userIcon: document.getElementById("user-icon"),
        accountPopup: document.getElementById("account-popup"),
        loginSection: document.getElementById("login-section"),
        userSection: document.getElementById("user-section"),
        userName: document.getElementById("user-name"),
        sidebarAccountInfo: document.getElementById("sidebar-account-info")
    };

    // Skeleton Loaders
    function showMenuItemSkeletons() {
        const skeletonCount = 8;
        DOM.menuItemsContainer.innerHTML = Array(skeletonCount).fill().map(() => `
            <div class="menu-item">
                <div class="item-image skeleton" style="width: 80px; height: 80px;"></div>
                <div class="item-details">
                    <div class="skeleton" style="width: 80%; height: 20px; margin-bottom: 8px;"></div>
                    <div class="skeleton" style="width: 60%; height: 16px;"></div>
                </div>
            </div>
        `).join('');
    }

    // Data Loading
    async function validateTable() {
        try {
            const url = API.validateTable(state.tableId);
            const result = await utils.fetchData(url);
            if (result.success) {
                state.tableName = result.data.tableName;
                DOM.tableName.textContent = state.tableName;
                DOM.customerName.textContent = result.data.fullName || "Khách QR";
                DOM.customerPhone.textContent = result.data.phone || "-";
            } else {
                utils.showToast(result.message, "error");
                DOM.submitOrderBtn.disabled = true;
            }
        } catch (error) {
            console.error('Validate table error:', error);
            utils.showToast("Lỗi khi kiểm tra bàn", "error");
            DOM.submitOrderBtn.disabled = true;
        }
    }

    async function loadCategories() {
        try {
            const result = await utils.fetchData(API.categories);
            if (result.success) {
                state.categories = result.data;
                renderCategoryTabs();
            } else {
                utils.showToast("Không thể tải danh mục", "error");
            }
        } catch (error) {
            console.error('Load categories error:', error);
            utils.showToast("Không thể tải danh mục", "error");
        }
    }

    async function loadMenuItems(page = state.currentPage, searchTerm = state.searchTerm, categoryId = state.categoryFilter) {
        try {
            showMenuItemSkeletons();
            state.searchTerm = searchTerm;
            state.categoryFilter = categoryId;
            state.currentPage = page;

            const url = API.menuItems(page, 4, searchTerm, categoryId);
            const result = await utils.fetchData(url);

            if (result.success) {
                state.menuItems = result.data;
                state.totalPages = result.pagination.totalPages;
                renderMenuItems();
                renderPagination();
            } else {
                utils.showToast("Không thể tải danh sách món ăn", "error");
            }
        } catch (error) {
            console.error('Load menu items error:', error);
            utils.showToast("Không thể tải danh sách món ăn", "error");
        }
    }

    // Rendering Functions
    function renderCategoryTabs() {
        DOM.categoryTabs.innerHTML = `
            <button class="tab-button active" data-tab="all" role="tab" aria-selected="true">Tất cả</button>
            ${state.categories.map(category => `
                <button class="tab-button" data-tab="${category.categoryID}" role="tab" aria-selected="false">${category.categoryName}</button>
            `).join('')}
        `;
    }

    function renderMenuItems() {
        DOM.menuItemsContainer.innerHTML = state.menuItems.length === 0 ? `
            <div class="text-center py-4">
                <svg class="text-muted mb-2" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path>
                </svg>
                <p class="text-muted">Không tìm thấy món ăn</p>
            </div>
        ` : state.menuItems.map(item => `
            <div class="menu-item">
                <div class="item-image">
                    ${item.menuItemImage ? 
                        `<img src="/images/MenuItem/${item.menuItemImage}" alt="${item.name}">` :
                        `<div class="no-image-placeholder"><svg class="text-muted" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect><circle cx="8.5" cy="8.5" r="1.5"></circle><polyline points="21 15 16 10 5 21"></polyline></svg><span>Không có ảnh</span></div>`}
                </div>
                <div class="item-details">
                    <h3 class="item-name">${item.name}</h3>
                    <p class="item-price">${utils.formatMoney(item.price)}</p>
                </div>
                <div class="quantity-controls">
                    ${item.status === 1 ? `
                        <button class="quantity-button add-to-order-btn" data-item-id="${item.menuItemID}">+</button>
                    ` : `
                        <span class="text-muted">Hết hàng</span>
                    `}
                </div>
            </div>
        `).join('');
    }

    function renderOrderItems() {
        DOM.selectedItemsContainer.innerHTML = state.orderItems.length === 0 ? `
            <p class="empty-selection">Chưa có món nào được chọn.</p>
        ` : `
            <table class="selected-items-table">
                <thead>
                    <tr>
                        <th>Món</th>
                        <th>Số lượng</th>
                        <th>Giá</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody>
                    ${state.orderItems.map((item, index) => `
                        <tr>
                            <td>
                                <div class="d-flex align-items-center">
                                    <div class="selected-item-image">
                                        ${item.menuItemImage ? 
                                            `<img src="/images/MenuItem/${item.menuItemImage}" alt="${item.name}">` :
                                            `<div class="no-image-placeholder"><svg class="text-muted" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect><circle cx="8.5" cy="8.5" r="1.5"></circle><polyline points="21 15 16 10 5 21"></polyline></svg></div>`}
                                    </div>
                                    <div>
                                        <strong style="word-break: break-word;">${item.name}</strong>
                                        <input type="text" class="form-control form-control-sm note-input" 
                                            placeholder="Ghi chú (VD: không hành)" 
                                            value="${item.note || ''}" 
                                            data-index="${index}"
                                            oninput="updateOrderItemNote(this)">
                                    </div>
                                </div>
                            </td>
                            <td>
                                <div class="quantity-controls">
                                    <button class="quantity-button" data-action="decrease" data-index="${index}">-</button>
                                    <span class="quantity-display">${item.quantity}</span>
                                    <button class="quantity-button" data-action="increase" data-index="${index}">+</button>
                                </div>
                            </td>
                            <td>${utils.formatMoney(item.price * item.quantity)}</td>
                            <td>
                                <button class="btn btn-outline-danger btn-sm" data-action="remove" data-index="${index}">
                                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                        <polyline points="3 6 5 6 21 6"></polyline>
                                        <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                                    </svg>
                                </button>
                            </td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        `;
        updateOrderTotal();
        updateSubmitButtonState();
    }

    function renderPagination() {
        const maxPagesToShow = 5;
        const startPage = Math.max(1, state.currentPage - Math.floor(maxPagesToShow / 2));
        const endPage = Math.min(state.totalPages, startPage + maxPagesToShow - 1);
        const adjustedStartPage = Math.max(1, endPage - maxPagesToShow + 1);

        DOM.menuPaginationPrev.classList.toggle("disabled", state.currentPage === 1);
        DOM.menuPaginationNext.classList.toggle("disabled", state.currentPage === state.totalPages);

        DOM.menuPaginationItems.innerHTML = Array.from({ length: endPage - adjustedStartPage + 1 }, (_, i) => {
            const page = adjustedStartPage + i;
            return `
                <li class="page-item ${page === state.currentPage ? 'active' : ''}">
                    <a class="page-link" href="#" data-page="${page}">${page}</a>
                </li>
            `;
        }).join('');
    }

    function updateOrderTotal() {
        const total = state.orderItems.reduce((sum, item) => sum + item.price * item.quantity, 0);
        DOM.totalPrice.textContent = utils.formatMoney(total);
    }

    function updateSubmitButtonState() {
        DOM.submitOrderBtn.disabled = state.orderItems.length === 0 || !state.tableName;
    }

    // Order Management
    window.updateOrderItemNote = (inputElement) => {
        const index = parseInt(inputElement.getAttribute('data-index'));
        if (!isNaN(index) && index >= 0 && index < state.orderItems.length) {
            state.orderItems[index].note = inputElement.value;
        }
    };

    window.updateOrderItemQuantity = (index, delta) => {
        const item = state.orderItems[index];
        if (item) {
            item.quantity = Math.max(1, item.quantity + delta);
            renderOrderItems();
        }
    };

    window.removeOrderItem = (index) => {
        if (index >= 0 && index < state.orderItems.length) {
            state.orderItems.splice(index, 1);
            renderOrderItems();
        }
    };

    async function submitOrder(e) {
        e.preventDefault();
        try {
            utils.showLoadingOverlay(true);

            const order = {
                TableID: state.tableId,
                OrderDetails: state.orderItems.map(item => ({
                    MenuItemID: item.menuItemID,
                    Quantity: item.quantity,
                    Note: item.note || ''
                }))
            };

            const response = await fetch(API.createOrder, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' },
                body: JSON.stringify(order)
            });

            const result = await response.json();
            if (result.success) {
                utils.showToast("Đặt món thành công", "success");
                state.orderItems = [];
                renderOrderItems();
                setTimeout(() => window.location.href = "/Home/Index", 2000);
            } else {
                utils.showToast(result.message || "Lỗi khi đặt món", "error");
            }
        } catch (error) {
            console.error('Submit order error:', error);
            utils.showToast("Lỗi khi đặt món", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // User Authentication
    async function checkUserLoginStatus() {
        try {
            const response = await fetch(API.checkUserLogin, {
                method: 'GET',
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            
            const result = await response.json();
            if (result.success && result.data) {
                state.userInfo = result.data;
                updateUserInterface();
            }
        } catch (error) {
            console.error('Check login status error:', error);
        }
    }

    function updateUserInterface() {
        if (state.userInfo) {
            // Cập nhật thông tin người dùng đã đăng nhập
            if (DOM.loginSection) DOM.loginSection.style.display = 'none';
            if (DOM.userSection) {
                DOM.userSection.style.display = 'block';
                DOM.userName.textContent = state.userInfo.fullName || state.userInfo.email;
            }
            
            // Cập nhật thông tin sidebar
            if (DOM.sidebarAccountInfo) {
                DOM.sidebarAccountInfo.innerHTML = `
                    <p>Đã đăng nhập với tài khoản:</p>
                    <p class="fw-bold">${state.userInfo.email}</p>
                    <div class="d-grid">
                        <a href="/User/Profile" class="btn btn-sm btn-outline-primary">Xem tài khoản</a>
                    </div>
                `;
            }
            
            // Cập nhật thông tin khách hàng nếu chưa có
            if (DOM.customerName.textContent === "Khách QR" && state.userInfo.fullName) {
                DOM.customerName.textContent = state.userInfo.fullName;
            }
            
            if (DOM.customerPhone.textContent === "-" && state.userInfo.phone) {
                DOM.customerPhone.textContent = state.userInfo.phone;
            }
        }
    }

    // Event Listeners
    function setupEventListeners() {
        let menuItemSearchTimeout;

        DOM.menuItemSearchInput.addEventListener("input", function () {
            clearTimeout(menuItemSearchTimeout);
            menuItemSearchTimeout = setTimeout(() => loadMenuItems(1, this.value, state.categoryFilter), 300);
        });

        DOM.categoryTabs.addEventListener("click", (e) => {
            const tabButton = e.target.closest(".tab-button");
            if (tabButton) {
                DOM.categoryTabs.querySelectorAll(".tab-button").forEach(btn => {
                    btn.classList.remove("active");
                    btn.setAttribute("aria-selected", "false");
                });
                tabButton.classList.add("active");
                tabButton.setAttribute("aria-selected", "true");

                const categoryId = tabButton.getAttribute("data-tab") === "all" ? 0 : parseInt(tabButton.getAttribute("data-tab"));
                loadMenuItems(1, state.searchTerm, categoryId);
            }
        });

        DOM.menuPagination.addEventListener("click", (e) => {
            e.preventDefault();
            const pageLink = e.target.closest(".page-link");
            if (pageLink && !pageLink.parentElement.classList.contains("disabled")) {
                const page = parseInt(pageLink.getAttribute("data-page")) || (pageLink.textContent === "Trước" ? state.currentPage - 1 : state.currentPage + 1);
                if (page > 0 && page <= state.totalPages) {
                    loadMenuItems(page);
                }
            }
        });

        DOM.menuItemsContainer.addEventListener("click", (e) => {
            const addToOrderBtn = e.target.closest(".add-to-order-btn");
            if (addToOrderBtn) {
                const itemId = parseInt(addToOrderBtn.getAttribute("data-item-id"));
                const item = state.menuItems.find(i => i.menuItemID === itemId);
                if (item) {
                    state.orderItems.push({
                        menuItemID: item.menuItemID,
                        name: item.name,
                        price: item.price,
                        quantity: 1,
                        note: '',
                        menuItemImage: item.menuItemImage
                    });
                    renderOrderItems();
                    // utils.showToast(`Đã thêm ${item.name} vào danh sách`, "success");
                }
            }
        });

        DOM.selectedItemsContainer.addEventListener("click", (e) => {
            const button = e.target.closest("button");
            if (!button) return;

            const index = parseInt(button.getAttribute("data-index"));
            if (isNaN(index)) return;

            const action = button.getAttribute("data-action");
            if (action === "increase") {
                updateOrderItemQuantity(index, 1);
            } else if (action === "decrease") {
                updateOrderItemQuantity(index, -1);
            } else if (action === "remove") {
                removeOrderItem(index);
            }
        });

        DOM.menuForm.addEventListener("submit", submitOrder);

        // Scroll to top button visibility
        window.addEventListener("scroll", () => {
            DOM.scrollTopButton.classList.toggle("visible", window.scrollY > 300);
        });

        DOM.scrollTopButton.addEventListener("click", () => {
            window.scrollTo({ top: 0, behavior: "smooth" });
        });
    }

    // Initialize
    async function initialize() {
        try {
            // Get tableId from URL
            const urlParams = new URLSearchParams(window.location.search);
            state.tableId = parseInt(urlParams.get("tableId")) || 0;

            if (state.tableId <= 0) {
                utils.showToast("Bàn không hợp lệ", "error");
                DOM.submitOrderBtn.disabled = true;
                return;
            }

            await Promise.all([
                validateTable(),
                loadCategories(),
                loadMenuItems(),
                checkUserLoginStatus()
            ]);

            setupEventListeners();
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi khởi tạo trang", "error");
        }
    }

    // Start the application
    initialize();
});