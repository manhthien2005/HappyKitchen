document.addEventListener('DOMContentLoaded', function () {
    const rowsPerPage = 5;
    const table = document.getElementById('reservationTable');
    const tbody = document.getElementById('reservationTableBody');
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

    document.querySelectorAll('.view-reservation').forEach(button => {
        button.addEventListener('click', function () {
            const reservationId = this.getAttribute('data-reservation-id');
            const modal = document.getElementById(`reservationModal-${reservationId}`);
            if (modal) {
                modal.style.display = 'flex';
            } else {
                console.warn('Modal not found for reservation ID:', reservationId);
                alert('Không tìm thấy chi tiết đặt bàn cho ID: ' + reservationId);
            }
        });
    });

    document.querySelectorAll('.view-order').forEach(button => {
        button.addEventListener('click', function () {
            const orderId = this.getAttribute('data-order-id');
            if (orderId) {
                window.location.href = `/User/ViewOrderHistory?search=${encodeURIComponent('#' + orderId)}`;
            } else {
                alert('Không tìm thấy đơn hàng liên quan.');
            }
        });
    });

    document.querySelectorAll('.close-modal').forEach(closeButton => {
        closeButton.addEventListener('click', function () {
            const reservationId = this.getAttribute('data-reservation-id');
            const modal = document.getElementById(`reservationModal-${reservationId}`);
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

    displayPage(1);
});