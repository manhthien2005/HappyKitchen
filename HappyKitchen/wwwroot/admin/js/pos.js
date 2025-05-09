document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        tables: [],
        customers: [],
        menuItems: [],
        selectedTable: null,
        selectedCustomer: null,
        selectedPaymentMethod: 0,
        orderItems: [],
        categories: [],
        searchTerm: "",
        categoryFilter: 0,
        currentPage: 1,
        totalPages: 1,
        customerSearchTerm: ""
    };

    // API Endpoints
    const API = {
        tables: '/Pos/GetTables',
        customers: (searchTerm) => `/Pos/GetCustomers?searchTerm=${encodeURIComponent(searchTerm)}`,
        menuItems: (page, pageSize, searchTerm, categoryId) =>
            `/Pos/GetMenuItems?page=${page}&pageSize=${pageSize}` +
            (searchTerm ? `&searchTerm=${encodeURIComponent(searchTerm)}` : '') +
            (categoryId > 0 ? `&categoryId=${categoryId}` : ''),
        categories: '/Pos/GetCategories',
        createOrder: '/Pos/CreateOrder'
    };

    // DOM Elements
    const DOM = {
        tableSelect: document.getElementById("tableSelect"),
        customerSearchInput: document.getElementById("customerSearchInput"),
        customerIdInput: document.getElementById("customerIdInput"),
        customerSearchResults: document.getElementById("customerSearchResults"),
        paymentMethodSelect: document.getElementById("paymentMethodSelect"),
        menuItemsGrid: document.getElementById("menuItemsGrid"),
        menuItemSearchInput: document.getElementById("menuItemSearchInput"),
        categoryFilter: document.getElementById("categoryFilter"),
        orderItems: document.getElementById("orderItems"),
        orderTotal: document.getElementById("orderTotal"),
        submitOrderBtn: document.getElementById("submitOrderBtn"),
        confirmTableName: document.getElementById("confirmTableName"),
        confirmPaymentMethod: document.getElementById("confirmPaymentMethod"),
        confirmOrderTotal: document.getElementById("confirmOrderTotal"),
        confirmSubmitOrderBtn: document.getElementById("confirmSubmitOrderBtn"),
        pagination: document.getElementById("pagination")
    };

    // Skeleton Loaders
    function showMenuItemSkeletons() {
        const skeletonCount = 9;
        DOM.menuItemsGrid.innerHTML = Array(skeletonCount).fill().map(() => `
            <div class="col">
                <div class="menu-item-card">
                    <div class="menu-item-card-image skeleton" style="height: 150px;"></div>
                    <div class="menu-item-card-body">
                        <h5 class="skeleton" style="width: 80%; height: 24px;"></h5>
                        <div class="skeleton" style="width: 60%; height: 16px;"></div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    // Data Loading
    async function loadTables() {
        try {
            const result = await utils.fetchData(API.tables);
            if (result.success) {
                state.tables = result.data;
                renderTables();
                const availableTable = state.tables.find(t => t.status === 1);
                if (availableTable) {
                    state.selectedTable = availableTable;
                    DOM.tableSelect.value = availableTable.tableID;
                    DOM.confirmTableName.textContent = availableTable.tableName;
                    updateSubmitButtonState();
                }
            } else {
                utils.showToast("Không thể tải danh sách bàn", "error");
            }
        } catch (error) {
            console.error('Load tables error:', error);
            utils.showToast("Không thể tải danh sách bàn", "error");
        }
    }

    async function loadCustomers(searchTerm = "") {
        try {
            const result = await utils.fetchData(API.customers(searchTerm));
            if (result.success) {
                state.customers = result.data;
                renderCustomerResults();
            } else {
                utils.showToast("Không thể tải danh sách khách hàng", "error");
            }
        } catch (error) {
            console.error('Load customers error:', error);
            utils.showToast("Không thể tải danh sách khách hàng", "error");
        }
    }

    async function loadCategories() {
        try {
            const result = await utils.fetchData(API.categories);
            if (result.success) {
                state.categories = result.data;
                renderCategoryOptions();
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

            const url = API.menuItems(page, 9, searchTerm, categoryId);
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
    function renderTables() {
        DOM.tableSelect.innerHTML = `
            <option value="0">Chọn bàn...</option>
            ${state.tables.map(table => `
                <option value="${table.tableID}" 
                    class="${table.status === 0 ? 'text-success' : table.status === 1 ? 'text-warning' : 'text-danger'}"
                    ${table.status === 2 ? 'disabled' : ''}>
                    ${table.tableName} (${table.status === 0 ? 'Trống' : table.status === 1 ? 'Đã đặt trước' : 'Đang sử dụng'})
                </option>
            `).join('')}
        `;
    }

    function renderCustomerResults() {
        DOM.customerSearchResults.innerHTML = state.customers.length === 0 ? `
            <div class="customer-search-result-item text-muted">Không tìm thấy khách hàng</div>
        ` : state.customers.map(customer => `
            <div class="customer-search-result-item" data-customer-id="${customer.userID}">
                ${customer.fullName} (${customer.phoneNumber})
            </div>
        `).join('');
        DOM.customerSearchResults.style.display = state.customers.length > 0 || state.customerSearchTerm ? 'block' : 'none';
    }

    function renderCategoryOptions() {
        DOM.categoryFilter.innerHTML = `
            <option value="0">Tất cả danh mục</option>
            ${state.categories.map(category => `
                <option value="${category.categoryID}">${category.categoryName}</option>
            `).join('')}
        `;
    }

    function renderMenuItems() {
        DOM.menuItemsGrid.innerHTML = state.menuItems.length === 0 ? `
            <div class="col-12 d-flex align-items-center justify-content-center w-100" style="min-height: 200px">
                <div class="text-center">
                    <i class="fas fa-box-open fa-3x text-muted mb-3"></i>
                    <p class="text-muted mb-0">Không tìm thấy món ăn</p>
                </div>
            </div>
        ` : state.menuItems.map(item => `
            <div class="col">
                <div class="menu-item-card">
                    <div class="menu-item-card-image">
                        <div class="menu-item-status-badge ${item.status === 1 ? 'status-badge-active' : 'status-badge-inactive'}">
                            ${item.status === 1 ? 'Còn hàng' : 'Hết hàng'}
                        </div>
                        ${item.menuItemImage ? 
                            `<img src="/images/MenuItem/${item.menuItemImage}" alt="${item.name}">` :
                            `<div class="no-image-placeholder"><i class="fas fa-image"></i><span>Không có ảnh</span></div>`}
                    </div>
                    <div class="menu-item-card-body">
                        <h5 class="menu-item-card-title">${item.name}</h5>
                        <div class="menu-item-card-price">${utils.formatMoney(item.price)}</div>
                        <button class="btn btn-primary btn-lg add-to-order-btn" data-item-id="${item.menuItemID}" ${item.status === 0 ? 'disabled' : ''}>
                            <i class="fas fa-plus me-2"></i> Thêm
                        </button>
                    </div>
                </div>
            </div>
        `).join('');
    }

    function renderOrderItems() {
        const clearCartButton = state.orderItems.length > 0 ? `
            <div class="d-flex justify-content-end mb-2">
                <button id="clearCartBtn" class="btn btn-outline-danger btn-sm">
                    <i class="fas fa-trash me-1"></i> Xóa hết giỏ hàng
                </button>
            </div>
        ` : '';

        DOM.orderItems.innerHTML = state.orderItems.length === 0 ? `
            <p class="text-muted">Giỏ hàng trống</p>
        ` : `
            ${clearCartButton}
            ${state.orderItems.map((item, index) => `
            <div class="order-item mb-3">
                <div class="d-flex align-items-start mb-1">
                    <div class="order-item-image me-3">
                        ${item.menuItemImage ? 
                            `<img src="/images/MenuItem/${item.menuItemImage}" alt="${item.name}">` :
                            `<div class="no-image-placeholder"><i class="fas fa-image"></i><span>Không có ảnh</span></div>`}
                    </div>
                    <div class="flex-grow-1">
                        <div class="d-flex justify-content-between align-items-center mb-1">
                            <div>
                                <strong>${item.name}</strong>
                                <div><small>${utils.formatMoney(item.price)} x ${item.quantity}</small></div>
                            </div>
                            <div class="d-flex align-items-center">
                                <button class="btn btn-outline-secondary btn-sm me-2" data-action="decrease" data-index="${index}">-</button>
                                <span>${item.quantity}</span>
                                <button class="btn btn-outline-secondary btn-sm ms-2" data-action="increase" data-index="${index}">+</button>
                                <button class="btn btn-outline-danger btn-sm ms-2" data-action="remove" data-index="${index}">
                                    <i class="fas fa-trash"></i>
                                </button>
                            </div>
                        </div>
                        <div class="mt-1">
                            <input type="text" class="form-control form-control-sm note-input" 
                                placeholder="Ghi chú (VD: không hành)" 
                                value="${item.note || ''}" 
                                data-index="${index}"
                                oninput="updateOrderItemNote(this)">
                        </div>
                    </div>
                </div>
            </div>
        `).join('')}
        `;
        updateOrderTotal();
        updateSubmitButtonState();
    }

    function renderPagination() {
        const maxPagesToShow = 5;
        const startPage = Math.max(1, state.currentPage - Math.floor(maxPagesToShow / 2));
        const endPage = Math.min(state.totalPages, startPage + maxPagesToShow - 1);
        const adjustedStartPage = Math.max(1, endPage - maxPagesToShow + 1);

        DOM.pagination.innerHTML = `
            <li class="page-item ${state.currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${state.currentPage - 1}">
                    <i class="fas fa-chevron-left"></i>
                </a>
            </li>
            ${Array.from({ length: endPage - adjustedStartPage + 1 }, (_, i) => adjustedStartPage + i).map(page => `
                <li class="page-item ${page === state.currentPage ? 'active' : ''}">
                    <a class="page-link" href="#" data-page="${page}">${page}</a>
                </li>
            `).join('')}
            <li class="page-item ${state.currentPage === state.totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${state.currentPage + 1}">
                    <i class="fas fa-chevron-right"></i>
                </a>
            </li>
        `;
    }

    function updateOrderTotal() {
        const total = state.orderItems.reduce((sum, item) => sum + item.price * item.quantity, 0);
        DOM.orderTotal.textContent = utils.formatMoney(total);
        DOM.confirmOrderTotal.textContent = utils.formatMoney(total);
    }

    function updateSubmitButtonState() {
        DOM.submitOrderBtn.disabled = !state.selectedTable || state.orderItems.length === 0;
    }

    // Order Management
    window.updateOrderItemQuantity = (index, delta) => {
        const item = state.orderItems[index];
        if (item) {
            item.quantity = Math.max(1, item.quantity + delta);
            renderOrderItems();
        }
    };

    window.updateOrderItemNote = (inputElement) => {
        const index = parseInt(inputElement.getAttribute('data-index'));
        if (!isNaN(index) && index >= 0 && index < state.orderItems.length) {
            state.orderItems[index].note = inputElement.value;
        }
    };

    window.removeOrderItem = (index) => {
        if (index >= 0 && index < state.orderItems.length) {
            state.orderItems.splice(index, 1);
            renderOrderItems();
        }
    };
    
    window.clearCart = () => {
        if (state.orderItems.length > 0) {
            if (confirm('Bạn có chắc muốn xóa hết giỏ hàng?')) {
                state.orderItems = [];
                renderOrderItems();
                utils.showToast('Đã xóa hết giỏ hàng', 'success');
            }
        }
    };

    function openConfirmOrderModal() {
        DOM.confirmPaymentMethod.textContent = DOM.paymentMethodSelect.options[DOM.paymentMethodSelect.selectedIndex].text;
        const modal = new bootstrap.Modal(document.getElementById("confirmOrderModal"));
        modal.show();
    }

    async function submitOrder() {
        try {
            utils.showLoadingOverlay(true);
            
            const order = {
                TableID: state.selectedTable.tableID,
                CustomerID: parseInt(DOM.customerIdInput.value) || null,
                PaymentMethod: state.selectedPaymentMethod,
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
                state.selectedCustomer = null;
                state.orderItems = [];
                DOM.customerSearchInput.value = "";
                DOM.customerIdInput.value = "0";
                DOM.paymentMethodSelect.value = 0;
                state.selectedPaymentMethod = 0;
                DOM.confirmTableName.textContent = state.selectedTable.tableName;
                renderOrderItems();
                loadTables();
                bootstrap.Modal.getInstance(document.getElementById('confirmOrderModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi đặt món", "error");
                if (result.message === "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.") {
                    window.location.href = "/Admin/Login";
                }
            }
        } catch (error) {
            console.error('Submit order error:', error);
            utils.showToast("Bạn không thể sử dụng tính năng này!", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Event Listeners
    function setupEventListeners() {
        let menuItemSearchTimeout;
        let customerSearchTimeout;

        DOM.tableSelect.addEventListener("change", function () {
            const tableId = parseInt(this.value);
            state.selectedTable = tableId === 0 ? null : state.tables.find(t => t.tableID === tableId);
            DOM.confirmTableName.textContent = state.selectedTable ? state.selectedTable.tableName : "";
            updateSubmitButtonState();
        });

        DOM.customerSearchInput.addEventListener("input", function () {
            clearTimeout(customerSearchTimeout);
            state.customerSearchTerm = this.value.trim();
            customerSearchTimeout = setTimeout(() => loadCustomers(state.customerSearchTerm), 300);
        });

        DOM.customerSearchInput.addEventListener("focus", function () {
            loadCustomers(state.customerSearchTerm);
        });

        DOM.customerSearchResults.addEventListener("click", function (e) {
            const resultItem = e.target.closest(".customer-search-result-item");
            if (resultItem) {
                const customerId = parseInt(resultItem.getAttribute("data-customer-id"));
                if (customerId) {
                    const customer = state.customers.find(c => c.userID === customerId);
                    state.selectedCustomer = customer;
                    DOM.customerSearchInput.value = `${customer.fullName} (${customer.phoneNumber})`;
                    DOM.customerIdInput.value = customer.userID;
                } else {
                    state.selectedCustomer = null;
                    DOM.customerSearchInput.value = "";
                    DOM.customerIdInput.value = "0";
                }
                DOM.customerSearchResults.style.display = 'none';
            }
        });

        document.addEventListener("click", function (e) {
            if (!DOM.customerSearchInput.contains(e.target) && !DOM.customerSearchResults.contains(e.target)) {
                DOM.customerSearchResults.style.display = 'none';
            }
        });

        DOM.paymentMethodSelect.addEventListener("change", function () {
            state.selectedPaymentMethod = this.value;
            DOM.confirmPaymentMethod.textContent = this.options[this.selectedIndex].text;
        });

        DOM.menuItemSearchInput.addEventListener("input", function () {
            clearTimeout(menuItemSearchTimeout);
            menuItemSearchTimeout = setTimeout(() => loadMenuItems(1, this.value, state.categoryFilter), 300);
        });

        DOM.categoryFilter.addEventListener("change", function () {
            loadMenuItems(1, state.searchTerm, parseInt(this.value));
        });

        DOM.submitOrderBtn.addEventListener("click", openConfirmOrderModal);

        DOM.confirmSubmitOrderBtn.addEventListener("click", submitOrder);

        DOM.pagination.addEventListener("click", (e) => {
            e.preventDefault();
            const pageLink = e.target.closest(".page-link");
            if (pageLink && !pageLink.parentElement.classList.contains("disabled")) {
                const page = parseInt(pageLink.getAttribute("data-page"));
                if (page > 0 && page <= state.totalPages) {
                    loadMenuItems(page);
                }
            }
        });

        // Xử lý các nút tương tác với giỏ hàng
        DOM.orderItems.addEventListener("click", (e) => {
            const button = e.target.closest("button");
            if (!button) return;
            
            // Xử lý nút xóa hết giỏ hàng
            if (button.id === "clearCartBtn") {
                clearCart();
                return;
            }
            
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

        // Xử lý khi thêm món vào giỏ hàng
        document.addEventListener("click", (e) => {
            const addToOrderBtn = e.target.closest(".add-to-order-btn");
            if (addToOrderBtn) {
                const itemId = parseInt(addToOrderBtn.getAttribute("data-item-id"));
                const item = state.menuItems.find(i => i.menuItemID === itemId);
                if (item) {
                    // Kiểm tra xem món đã có trong giỏ hàng chưa, nếu đã có thì tạo món mới với ghi chú rỗng
                    const existingItemIndex = state.orderItems.findIndex(oi => oi.menuItemID === itemId);
                    
                    if (existingItemIndex >= 0) {
                        // Tạo một món mới để người dùng có thể thêm ghi chú khác
                        state.orderItems.push({
                            menuItemID: item.menuItemID,
                            name: item.name,
                            price: item.price,
                            quantity: 1,
                            note: '',
                            menuItemImage: item.menuItemImage
                        });
                    } else {
                        // Thêm món mới vào giỏ hàng
                        state.orderItems.push({
                            menuItemID: item.menuItemID,
                            name: item.name,
                            price: item.price,
                            quantity: 1,
                            note: '',
                            menuItemImage: item.menuItemImage
                        });
                    }
                    renderOrderItems();
                    
                    // utils.showToast(`Đã thêm ${item.name} vào giỏ hàng`, "success");
                }
            }
        });
    }

    // Initialize
    async function initialize() {
        try {
            await Promise.all([
                loadTables(),
                loadCategories(),
                loadMenuItems()
            ]);
            DOM.paymentMethodSelect.value = state.selectedPaymentMethod;
            DOM.confirmPaymentMethod.textContent = DOM.paymentMethodSelect.options[DOM.paymentMethodSelect.selectedIndex].text;
            setupEventListeners();
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi khởi tạo trang", "error");
        }
    }

    // Start the application
    initialize();
});