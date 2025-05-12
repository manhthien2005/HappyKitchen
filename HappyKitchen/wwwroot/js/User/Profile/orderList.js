document.addEventListener('DOMContentLoaded', function () {
    const rowsPerPage = 5;
    const table = document.getElementById('orderTable');
    const tbody = document.getElementById('orderTableBody');
    const rows = Array.from(tbody.getElementsByTagName('tr'));
    const pagination = document.getElementById('pagination');
    const searchInput = document.getElementById('searchInput');
    let currentPage = 1;
    let filteredRows = rows;

    // Phân trang
    function displayPage(page) {
        const start = (page - 1) * rowsPerPage;
        const end = start + rowsPerPage;
        rows.forEach(row => row.style.display = 'none');
        filteredRows.slice(start, end).forEach(row => row.style.display = '');
        updatePagination();
    }

    function updatePagination() {
        const pageCount = Math.ceil(filteredRows.length / rowsPerPage);
        pagination.innerHTML = '';
        for (let i = 1; i <= pageCount; i++) {
            const button = document.createElement('button');
            button.textContent = i;
            button.className = i === currentPage ? 'active' : '';
            button.addEventListener('click', () => {
                currentPage = i;
                displayPage(i);
            });
            pagination.appendChild(button);
        }
    }

    // Tìm kiếm
    searchInput.addEventListener('input', function () {
        const searchText = this.value.toLowerCase();
        filteredRows = rows.filter(row => {
            const id = row.cells[0].textContent.toLowerCase();
            const date = row.cells[1].textContent.toLowerCase();
            return id.includes(searchText) || date.includes(searchText);
        });
        currentPage = 1;
        displayPage(1);
    });

    // Hiển thị modal
    const modal = document.getElementById('orderModal');
    const closeModal = document.querySelector('.close-modal');
    const modalOrderId = document.getElementById('modalOrderId');
    const modalOrderDate = document.getElementById('modalOrderDate');
    const modalPaymentMethod = document.getElementById('modalPaymentMethod');
    const modalStatus = document.getElementById('modalStatus');
    const modalOrderDetailsBody = document.getElementById('modalOrderDetailsBody');
    const modalTotalPrice = document.getElementById('modalTotalPrice');

    document.querySelectorAll('.view-order').forEach(button => {
        button.addEventListener('click', function () {
            const orderId = this.getAttribute('data-order-id');
            const row = Array.from(rows).find(r => r.getAttribute('data-order-id') === orderId);
            const orderDetailsJson = row.getAttribute('data-order-details');
            const orderDetails = orderDetailsJson ? JSON.parse(orderDetailsJson) : [];

            modalOrderId.textContent = orderId;
            modalOrderDate.textContent = row.cells[1].textContent;
            modalPaymentMethod.textContent = row.cells[2].textContent;
            modalStatus.textContent = row.cells[4].textContent;

            modalOrderDetailsBody.innerHTML = '';
            orderDetails.forEach(item => {
                const tr = document.createElement('tr');
                tr.innerHTML = `
                    <td>${item.name || item.Name || 'Không có tên'}</td>
                    <td>${item.quantity || item.Quantity || 0}</td>
                    <td>${(item.price || item.Price || 0).toLocaleString()} VNĐ</td>
                    <td>${((item.quantity || item.Quantity || 0) * (item.price || item.Price || 0)).toLocaleString()} VNĐ</td>
                `;
                modalOrderDetailsBody.appendChild(tr);
            });

            const totalPrice = orderDetails.reduce((sum, item) => sum + ((item.quantity || item.Quantity || 0) * (item.price || item.Price || 0)), 0);
            modalTotalPrice.textContent = totalPrice.toLocaleString();

            modal.style.display = 'flex';
        });
    });

    closeModal.addEventListener('click', () => {
        modal.style.display = 'none';
    });

    window.addEventListener('click', (e) => {
        if (e.target === modal) {
            modal.style.display = 'none';
        }
    });

    // Hiển thị trang đầu tiên
    displayPage(1);
});