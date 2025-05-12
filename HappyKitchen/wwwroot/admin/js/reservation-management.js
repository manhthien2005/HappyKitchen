document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        reservations: [],
        pagination: {
            currentPage: 1,
            pageSize: 8,
            totalItems: 0,
            totalPages: 0
        },
        searchTerm: "",
        statusFilter: "all",
        startDate: "",
        endDate: "",
        searchInDetails: false
    };

    // API Endpoints
    const API = {
        reservations: (page, pageSize, searchTerm, statusFilter, startDate, endDate, searchInDetails) =>
            `/ReservationManage/GetReservations?page=${page}&pageSize=${pageSize}` +
            (searchTerm ? `&searchTerm=${encodeURIComponent(searchTerm)}` : '') +
            (statusFilter !== 'all' ? `&status=${statusFilter}` : '') +
            (startDate ? `&startDate=${encodeURIComponent(startDate)}` : '') +
            (endDate ? `&endDate=${encodeURIComponent(endDate)}` : '') +
            (searchInDetails ? `&searchInDetails=true` : ''),
        updateReservation: '/ReservationManage/UpdateReservation',
        deleteReservation: (id) => `/ReservationManage/DeleteReservation?id=${id}`
    };

    // DOM Elements
    const DOM = {
        reservationsGrid: document.getElementById("reservationsGrid"),
        reservationSearchInput: document.getElementById("reservationSearchInput"),
        startDateInput: document.getElementById("startDateInput"),
        endDateInput: document.getElementById("endDateInput"),
        clearFiltersBtn: document.getElementById("clearFiltersBtn"),
        filterResult: document.getElementById("filterResult"),
        updateReservationBtn: document.getElementById("updateReservationBtn"),
        paginationContainer: document.getElementById("reservationPagination"),
        paginationPrev: document.getElementById("paginationPrev"),
        paginationNext: document.getElementById("paginationNext"),
        paginationItems: document.getElementById("paginationItems"),
        statusFilter: document.getElementById("statusFilter")
    };

    // Skeleton Loaders
    function showReservationSkeletons() {
        const skeletonCount = 8;
        DOM.reservationsGrid.innerHTML = Array(skeletonCount).fill().map(() => `
            <tr>
                <td><div class="skeleton" style="width: 80%; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 60%; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 80%; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 60%; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 60%; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 80%; height: 20px;"></div></td>
            </tr>
        `).join('');
    }

    // Data Loading
    async function loadReservations(page = 1) {
        try {
            showReservationSkeletons();

            state.pagination.currentPage = page;

            const url = API.reservations(
                page,
                state.pagination.pageSize,
                state.searchTerm,
                state.statusFilter,
                state.startDate,
                state.endDate,
                state.searchInDetails
            );
            const result = await utils.fetchData(url);

            if (result.success) {
                state.reservations = result.data;
                state.pagination = result.pagination;
                renderReservations();
                renderPagination();
                updateFilterResult();
            } else {
                utils.showToast(result.message || "Không thể tải danh sách đơn đặt bàn", "error");
            }
        } catch (error) {
            console.error('Load reservations error:', error);
            utils.showToast("Không thể tải danh sách đơn đặt bàn", "error");
        }
    }

    // Rendering Functions
    function renderReservations() {
        DOM.reservationsGrid.innerHTML = state.reservations.length === 0
            ? `
                <tr>
                    <td colspan="6" class="text-center">
                        <div class="py-4">
                            <i class="fas fa-box-open fa-3x text-muted mb-3"></i>
                            <p class="text-muted mb-0">Không tìm thấy đơn đặt bàn</p>
                        </div>
                    </td>
                </tr>
            `
            : state.reservations.map(reservation => `
                <tr>
                    <td>#${reservation.reservationID}</td>
                    <td>${reservation.customerName}</td>
                    <td>${new Date(reservation.reservationTime).toLocaleString()}</td>
                    <td>${reservation.capacity}</td>
                    <td>
                        <span class="reservation-status-badge ${getStatusBadgeClass(reservation.status)}">
                            ${getStatusText(reservation.status)}
                        </span>
                    </td>
                    <td>
                        <button class="btn btn-sm btn-outline-primary edit-reservation-btn" data-reservation-id="${reservation.reservationID}" data-bs-toggle="modal" data-bs-target="#editReservationModal" title="Sửa đơn đặt bàn">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger delete-reservation-btn" data-reservation-id="${reservation.reservationID}" title="Xóa đơn đặt bàn">
                            <i class="fas fa-trash"></i>
                        </button>
                        ${reservation.orderID ? `
                            <button class="btn btn-sm btn-outline-info view-order-btn" data-order-id="${reservation.orderID}" title="Xem đơn hàng liên quan">
                                <i class="fas fa-eye"></i>
                            </button>
                        ` : ''}
                    </td>
                </tr>
            `).join('');
    }

    function getStatusText(status) {
        switch (status) {
            case 0: return 'Đã hủy';
            case 1: return 'Đã xác nhận';
            case 2: return 'Hoàn thành';
            default: return 'Không xác định';
        }
    }

    function getStatusBadgeClass(status) {
        switch (status) {
            case 0: return 'status-badge-inactive';
            case 1: return 'status-badge-preparing';
            case 2: return 'status-badge-active';
            default: return '';
        }
    }

    function renderPagination() {
        utils.renderPagination(
            state.pagination,
            DOM.paginationContainer,
            DOM.paginationItems,
            DOM.paginationPrev,
            DOM.paginationNext,
            (pageNum) => loadReservations(pageNum)
        );
    }

    function updateFilterResult() {
        DOM.filterResult.textContent = `Tìm thấy ${state.pagination.totalItems} đơn đặt bàn`;
    }

    // Form Reset Functions
    function resetEditForm() {
        const form = document.getElementById("editReservationForm");
        form.reset();
        console.log("Edit form reset");
    }

    function resetFilters() {
        state.searchTerm = "";
        state.statusFilter = "all";
        state.startDate = "";
        state.endDate = "";
        state.searchInDetails = false;

        DOM.reservationSearchInput.value = "";
        DOM.startDateInput.value = "";
        DOM.endDateInput.value = "";
        DOM.statusFilter.querySelectorAll("button").forEach(b => b.classList.remove("active"));
        DOM.statusFilter.querySelector("button[data-status='all']").classList.add("active");

        loadReservations(1);
    }

    // Event Listeners
    function setupEventListeners() {
        let searchTimeout;

        DOM.reservationSearchInput?.addEventListener("input", function () {
            clearTimeout(searchTimeout);
            state.searchTerm = this.value.toLowerCase();
            state.searchInDetails = state.searchTerm.length > 0;
            searchTimeout = setTimeout(() => loadReservations(1), 300);
        });

        DOM.startDateInput?.addEventListener("change", function () {
            state.startDate = this.value;
            loadReservations(1);
        });

        DOM.endDateInput?.addEventListener("change", function () {
            state.endDate = this.value;
            loadReservations(1);
        });

        DOM.clearFiltersBtn?.addEventListener("click", resetFilters);

        DOM.updateReservationBtn?.addEventListener("click", updateReservation);

        DOM.paginationPrev?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage > 1) {
                loadReservations(state.pagination.currentPage - 1);
            }
        });

        DOM.paginationNext?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage < state.pagination.totalPages) {
                loadReservations(state.pagination.currentPage + 1);
            }
        });

        DOM.statusFilter?.addEventListener("click", (e) => {
            const btn = e.target.closest("button[data-status]");
            if (btn) {
                DOM.statusFilter.querySelectorAll("button").forEach(b => b.classList.remove("active"));
                btn.classList.add("active");
                state.statusFilter = btn.getAttribute("data-status");
                loadReservations(1);
            }
        });

        document.addEventListener("click", (e) => {
            const editBtn = e.target.closest(".edit-reservation-btn");
            const deleteBtn = e.target.closest(".delete-reservation-btn");
            const viewOrderBtn = e.target.closest(".view-order-btn");

            if (editBtn) openEditReservationModal(parseInt(editBtn.getAttribute("data-reservation-id")));
            if (deleteBtn) deleteReservation(parseInt(deleteBtn.getAttribute("data-reservation-id")));
            if (viewOrderBtn) viewRelatedOrder(parseInt(viewOrderBtn.getAttribute("data-order-id")));
        });

        document.getElementById("editReservationModal")?.addEventListener("hidden.bs.modal", resetEditForm);
    }

    async function updateReservation() {
        console.log("Starting updateReservation function");
        const form = document.getElementById("editReservationForm");
        if (!form.checkValidity()) {
            console.log("Edit form validation failed");
            form.reportValidity();
            return;
        }
        console.log("Edit form validation passed");

        const reservation = {
            ReservationID: parseInt(document.getElementById("editReservationId").value),
            CustomerName: document.getElementById("editCustomerName").value.trim(),
            CustomerPhone: document.getElementById("editCustomerPhone").value.trim(),
            Duration: parseInt(document.getElementById("editDuration").value),
            Capacity: parseInt(document.getElementById("editCapacity").value),
            CreatedTime: new Date(document.getElementById("editCreatedTime").value).toISOString(),
            ReservationTime: new Date(document.getElementById("editReservationTime").value).toISOString(),
            Status: parseInt(document.getElementById("editReservationStatus").value),
            Notes: document.getElementById("editNotes").value.trim()
        };

        console.log("Updating reservation", { reservation });

        try {
            console.log("Showing loading overlay and starting update API request");
            utils.showLoadingOverlay(true);
            console.log("Sending POST request to:", API.updateReservation);

            const response = await fetch(API.updateReservation, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(reservation)
            });
            console.log("Update API response received:", {
                status: response.status,
                statusText: response.statusText
            });

            const result = await response.json();
            console.log("Update reservation result:", result);

            if (result.success) {
                console.log("Reservation updated successfully");
                utils.showToast("Cập nhật đơn đặt bàn thành công", "success");
                console.log("Reloading reservations after update");
                await loadReservations(state.pagination.currentPage);
                console.log("Resetting edit form");
                resetEditForm();
                console.log("Closing edit modal");
                bootstrap.Modal.getInstance(document.getElementById('editReservationModal')).hide();
            } else {
                console.error("Update API returned error:", result.message);
                utils.showToast(result.message || "Lỗi khi cập nhật đơn đặt bàn", "error");
            }
        } catch (error) {
            console.error("Update reservation error:", error);
            utils.showToast("Đã xảy ra lỗi khi cập nhật đơn đặt bàn", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function deleteReservation(reservationId) {
        const reservation = state.reservations.find(r => r.reservationID === reservationId);
        if (!reservation) return;

        console.log("Reservation to delete:", reservation.reservationID);
        document.getElementById("deleteReservationName").innerHTML = `#${reservation.reservationID}`;
        document.getElementById("deleteReservationId").value = reservation.reservationID;
        const deleteModal = new bootstrap.Modal(document.getElementById("deleteReservationModal"));
        deleteModal.show();

        const confirmBtn = document.getElementById("confirmDeleteReservationBtn");
        const deleteHandler = async () => {
            try {
                utils.showLoadingOverlay(true);
                console.log("Deleting reservation", { reservationId });
                const result = await utils.fetchData(API.deleteReservation(reservationId), 'POST');
                console.log("Delete reservation result", result);

                if (result.success) {
                    utils.showToast("Xóa đơn đặt bàn thành công", "success");
                    await loadReservations(state.pagination.currentPage);
                    deleteModal.hide();
                } else {
                    utils.showToast(result.message || "Lỗi khi xóa đơn đặt bàn", "error");
                }
            } catch (error) {
                console.error("Delete reservation error", error);
                utils.showToast("Đã xảy ra lỗi khi xóa đơn đặt bàn", "error");
            } finally {
                utils.showLoadingOverlay(false);
                confirmBtn.removeEventListener("click", deleteHandler);
                deleteModal._element.removeEventListener('hidden.bs.modal', deleteModalHandler);
            }
        };

        const deleteModalHandler = () => {
            confirmBtn.removeEventListener("click", deleteHandler);
        };

        confirmBtn.addEventListener("click", deleteHandler);
        deleteModal._element.addEventListener('hidden.bs.modal', deleteModalHandler, { once: true });
    }

    function viewRelatedOrder(orderId) {
        console.log("Viewing related order:", orderId);
        // Redirect hoặc mở modal hiển thị chi tiết đơn hàng
        // Ví dụ: redirect đến trang quản lý đơn hàng với OrderID
        window.location.href = `/OrderManage/Index?orderId=${orderId}`;
        // Hoặc mở modal tùy thuộc vào thiết kế hệ thống
        // utils.showToast(`Hiển thị đơn hàng #${orderId}`, "info");
    }

    async function openEditReservationModal(reservationId) {
        const reservation = state.reservations.find(r => r.reservationID === reservationId);
        if (!reservation) {
            console.error("Reservation not found for edit", { reservationId });
            utils.showToast("Không tìm thấy đơn đặt bàn", "error");
            return;
        }

        console.log("Opening edit modal for reservation", { reservationId });

        document.getElementById("editReservationId").value = reservation.reservationID;
        document.getElementById("editCustomerName").value = reservation.customerName;
        document.getElementById("editCustomerPhone").value = reservation.customerPhone;
        document.getElementById("editDuration").value = reservation.duration;
        document.getElementById("editCapacity").value = reservation.capacity;
        document.getElementById("editCreatedTime").value = new Date(reservation.createdTime).toISOString().slice(0, 16);
        document.getElementById("editReservationTime").value = new Date(reservation.reservationTime).toISOString().slice(0, 16);
        document.getElementById("editReservationStatus").value = reservation.status;
        document.getElementById("editNotes").value = reservation.notes || '';

        console.log("Edit modal populated with", {
            reservationId,
            customerName: reservation.customerName,
            customerPhone: reservation.customerPhone,
            duration: reservation.duration,
            capacity: reservation.capacity,
            createdTime: reservation.createdTime,
            reservationTime: reservation.reservationTime,
            status: reservation.status,
            notes: reservation.notes
        });
    }

    // Initialize
    async function initialize() {
        try {
            await loadReservations();
            setupEventListeners();
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi khởi tạo trang", "error");
        }
    }

    initialize();
});