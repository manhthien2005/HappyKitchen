document.addEventListener('DOMContentLoaded', function () {
    const rowsPerPage = 5; // Số đơn hàng mỗi trang (bảng chính)
    const detailsPerPage = 5; // Số chi tiết đơn hàng mỗi trang (trong modal)

    // Phân trang cho bảng đơn hàng chính
    const table = document.getElementById('orderTable');
    const tbody = document.getElementById('orderTableBody');
    const rows = Array.from(tbody.getElementsByTagName('tr'));
    const pagination = document.getElementById('pagination');
    const searchInput = document.getElementById('searchInput');
    let currentPage = 1;
    let filteredRows = rows;

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

    // Phân trang cho chi tiết đơn hàng trong modal
    function setupModalPagination(orderId) {
        const modalTable = document.getElementById(`modalOrderDetails-${orderId}`);
        const modalTbody = modalTable.querySelector('.modalOrderDetailsBody');
        const detailRows = Array.from(modalTbody.getElementsByTagName('tr'));
        const modalPagination = document.getElementById(`modalPagination-${orderId}`);
        let currentDetailPage = 1;

        function displayDetailPage(page) {
            const start = (page - 1) * detailsPerPage;
            const end = start + detailsPerPage;
            detailRows.forEach(row => row.style.display = 'none');
            detailRows.slice(start, end).forEach(row => row.style.display = '');
            updateModalPagination();
        }

        function updateModalPagination() {
            const pageCount = Math.ceil(detailRows.length / detailsPerPage);
            modalPagination.innerHTML = '';
            if (pageCount <= 1) return; // Không hiển thị phân trang nếu chỉ có 1 trang
            for (let i = 1; i <= pageCount; i++) {
                const button = document.createElement('button');
                button.textContent = i;
                button.className = i === currentDetailPage ? 'active' : '';
                button.addEventListener('click', () => {
                    currentDetailPage = i;
                    displayDetailPage(i);
                });
                modalPagination.appendChild(button);
            }
        }

        // Hiển thị trang đầu tiên
        displayDetailPage(1);
    }

    // Hiển thị modal
    document.querySelectorAll('.view-order').forEach(button => {
        button.addEventListener('click', function () {
            const orderId = this.getAttribute('data-order-id');
            const modal = document.getElementById(`orderModal-${orderId}`);

            if (modal) {
                modal.style.display = 'flex';
                setupModalPagination(orderId); // Thiết lập phân trang cho modal
            } else {
                console.warn('Modal not found for order ID:', orderId);
                alert('Không tìm thấy chi tiết đơn hàng cho ID: ' + orderId);
            }
        });
    });

    // Đóng modal
    document.querySelectorAll('.close-modal').forEach(closeButton => {
        closeButton.addEventListener('click', function () {
            const orderId = this.getAttribute('data-order-id');
            const modal = document.getElementById(`orderModal-${orderId}`);
            if (modal) {
                modal.style.display = 'none';
            }
        });
    });

    window.addEventListener('click', (e) => {
        document.querySelectorAll('.modal').forEach(modal => {
            if (e.target === modal) {
                modal.style.display = 'none';
            }
        });
    });

    // Hiển thị trang đầu tiên cho bảng đơn hàng chính
    displayPage(1);
});