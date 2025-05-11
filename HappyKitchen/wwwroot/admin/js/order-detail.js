document.addEventListener('DOMContentLoaded', function() {
    const orderId = new URLSearchParams(window.location.search).get('orderId');
    
    if (!orderId) {
        showError("Không tìm thấy mã đơn hàng");
        return;
    }
    
    loadOrderDetails(orderId);
    
    function loadOrderDetails(orderId) {
        fetch(`/CustomerManage/GetOrderDetails?orderId=${orderId}`)
            .then(response => response.json())
            .then(result => {
                if (result.success) {
                    renderOrderDetails(result.data);
                } else {
                    showError(result.message || "Không thể tải thông tin đơn hàng");
                }
            })
            .catch(error => {
                console.error('Error loading order details:', error);
                showError("Lỗi khi tải thông tin đơn hàng");
            });
    }
    
    function renderOrderDetails(data) {
        const { order, customer } = data;
        
        // Hiển thị thông tin đơn hàng
        document.getElementById('orderId').textContent = `#${order.orderID}`;
        document.getElementById('orderDate').textContent = formatDate(order.orderTime);
        document.getElementById('orderStatus').textContent = getOrderStatusText(order.status);
        document.getElementById('orderStatus').className = `order-status ${getOrderStatusClass(order.status)}`;
        
        // Hiển thị thông tin khách hàng
        document.getElementById('customerName').textContent = customer.fullName;
        document.getElementById('customerPhone').textContent = customer.phoneNumber;
        document.getElementById('customerEmail').textContent = customer.email;
        document.getElementById('customerAddress').textContent = customer.address;
        
        // Hiển thị danh sách sản phẩm
        const productsContainer = document.getElementById('orderProductsBody');
        productsContainer.innerHTML = '';
        
        order.items.forEach(item => {
            const productElement = document.createElement('div');
            productElement.className = 'product-item';
            productElement.innerHTML = `
                <div class="product-name">${item.name}</div>
                <div class="product-unit-price">${formatMoney(item.unitPrice)}</div>
                <div class="product-quantity">${item.quantity}</div>
                <div class="product-price">${formatMoney(item.price)}</div>
            `;
            productsContainer.appendChild(productElement);
        });
        
        // Hiển thị tổng tiền
        document.getElementById('orderTotal').textContent = formatMoney(order.total);
    }
    
    function formatDate(dateString) {
        const date = new Date(dateString);
        return `${date.getDate().toString().padStart(2, '0')}/${(date.getMonth() + 1).toString().padStart(2, '0')}/${date.getFullYear()} ${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
    }
    
    function formatMoney(amount) {
        return amount.toLocaleString('vi-VN') + ' đ';
    }
    
    function getOrderStatusText(status) {
        switch (status) {
            case 0: return "Đã hủy";
            case 1: return "Đang chờ";
            case 2: return "Đang chuẩn bị";
            case 3: return "Đang phục vụ";
            case 4: return "Hoàn thành";
            default: return "Không xác định";
        }
    }
    
    function getOrderStatusClass(status) {
        switch (status) {
            case 0: return "status-cancelled";
            case 1: return "status-pending";
            case 2: return "status-preparing";
            case 3: return "status-serving";
            case 4: return "status-completed";
            default: return "status-unknown";
        }
    }
    
    function showError(message) {
        const container = document.querySelector('.order-detail-container');
        container.innerHTML = `
            <div class="alert alert-danger">
                <i class="fas fa-exclamation-circle me-2"></i>
                ${message}
            </div>
        `;
    }
});