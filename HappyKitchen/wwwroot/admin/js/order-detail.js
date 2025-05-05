document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        order: null,
        customer: null
    };

    // API Endpoints
    const API = {
        getOrderDetails: (orderId) => `/CustomerManage/GetOrderDetails?orderId=${orderId}`
    };

    // DOM Elements
    const DOM = {
        orderId: document.getElementById("orderId"),
        orderDate: document.getElementById("orderDate"),
        orderStatus: document.getElementById("orderStatus"),
        customerName: document.getElementById("customerName"),
        customerPhone: document.getElementById("customerPhone"),
        customerEmail: document.getElementById("customerEmail"),
        customerAddress: document.getElementById("customerAddress"),
        orderProductsBody: document.getElementById("orderProductsBody"),
        orderTotal: document.getElementById("orderTotal"),
        printOrderBtn: document.getElementById("printOrderBtn")
    };

    // Utility Functions
    const utils = {
        formatMoney: (amount) => {
            return amount.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' });
        },
        fetchData: async (url, method = 'GET', data = null) => {
            const options = { method, headers: { 'Content-Type': 'application/json' } };
            if (data) options.body = JSON.stringify(data);
            const response = await fetch(url, options);
            return await response.json();
        },
        showToast: (message, type) => {
            // Placeholder for toast notification
            console.log(`${type}: ${message}`);
        },
        showLoadingOverlay: (show) => {
            // Placeholder for loading overlay
            console.log(show ? "Show loading" : "Hide loading");
        }
    };

    // Helper Functions
    function getOrderStatusText(status) {
        const statusMap = {
            0: "Chờ xử lý",
            1: "Đang xử lý",
            2: "Hoàn thành",
            3: "Đã hủy"
        };
        return statusMap[status] || "Chờ xử lý";
    }

    function getOrderStatusClass(status) {
        const statusMap = {
            0: "status-pending",
            1: "status-processing",
            2: "status-completed",
            3: "status-cancelled"
        };
        return statusMap[status] || "status-pending";
    }

    // Rendering Functions
    function renderOrderDetails() {
        if (!state.order || !state.customer) {
            DOM.orderProductsBody.innerHTML = `
                <div class="empty-orders">
                    <i class="fas fa-shopping-cart"></i>
                    <p>Không tìm thấy thông tin đơn hàng</p>
                </div>`;
            return;
        }

        DOM.orderId.textContent = `#${state.order.orderID}`;
        DOM.orderDate.textContent = new Date(state.order.orderTime).toLocaleString('vi-VN');
        DOM.orderStatus.textContent = getOrderStatusText(state.order.status);
        DOM.orderStatus.className = `order-status ${getOrderStatusClass(state.order.status)}`;

        DOM.customerName.textContent = state.customer.fullName || "Không có tên";
        DOM.customerPhone.textContent = state.customer.phoneNumber || "Chưa cập nhật";
        DOM.customerEmail.textContent = state.customer.email || "Chưa cập nhật";
        DOM.customerAddress.textContent = state.customer.address || "Chưa cập nhật";

        DOM.orderProductsBody.innerHTML = state.order.items.map(product => `
            <div class="product-item">
                <div class="product-name">${product.name}</div>
                <div class="product-unit-price">${utils.formatMoney(product.unitPrice || (product.price / product.quantity))}</div>
                <div class="product-quantity">x${product.quantity}</div>
                <div class="product-price">${utils.formatMoney(product.price)}</div>
            </div>
        `).join('');

        DOM.orderTotal.textContent = utils.formatMoney(state.order.total);
    }

    // Data Loading
    async function loadOrderDetails(orderId) {
        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.getOrderDetails(orderId));
            if (result.success) {
                state.order = result.data.order;
                state.customer = result.data.customer;
                renderOrderDetails();
            } else {
                utils.showToast(result.message || "Không thể tải chi tiết đơn hàng", "error");
                DOM.orderProductsBody.innerHTML = `
                    <div class="empty-orders">
                        <i class="fas fa-shopping-cart"></i>
                        <p>${result.message || "Không tìm thấy thông tin đơn hàng"}</p>
                    </div>`;
            }
        } catch (error) {
            console.error('Load order details error:', error);
            utils.showToast("Không thể tải chi tiết đơn hàng", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Event Listeners
    function setupEventListeners() {
        DOM.printOrderBtn.addEventListener("click", () => {
            if (state.order) {
                // Placeholder for print functionality
                alert(`Đơn hàng ${state.order.orderID} sẽ được in`);
            }
        });
    }

    // Initialization
    async function initialize() {
        try {
            const urlParams = new URLSearchParams(window.location.search);
            const orderId = urlParams.get('orderId');
            if (orderId) {
                await loadOrderDetails(orderId);
                setupEventListeners();
            } else {
                utils.showToast("Không tìm thấy ID đơn hàng", "error");
                DOM.orderProductsBody.innerHTML = `
                    <div class="empty-orders">
                        <i class="fas fa-shopping-cart"></i>
                        <p>Không tìm thấy thông tin đơn hàng</p>
                    </div>`;
            }
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi tải dữ liệu", "error");
        }
    }

    initialize();
});