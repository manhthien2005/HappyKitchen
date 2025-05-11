document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        areas: [],
        tables: [],
        areaPagination: {
            currentPage: 1,
            pageSize: 10,
            totalItems: 0,
            totalPages: 0
        },
        tablePagination: {
            currentPage: 1,
            pageSize: 10,
            totalItems: 0,
            totalPages: 0
        },
        areaSearchTerm: "",
        tableSearchTerm: "",
        tableAreaFilter: 0,
        tableStatusFilter: "all"
    };

    // API Endpoints
    const API = {
        areas: (searchTerm, page, pageSize) =>
            `/TableManage/GetAreas?searchTerm=${encodeURIComponent(searchTerm)}&page=${page}&pageSize=${pageSize}`,
        createArea: '/TableManage/CreateArea',
        updateArea: '/TableManage/UpdateArea',
        deleteArea: (id) => `/TableManage/DeleteArea?id=${id}`,
        tables: (searchTerm, areaId, status, page, pageSize) =>
            `/TableManage/GetTables?searchTerm=${encodeURIComponent(searchTerm)}` +
            (areaId > 0 ? `&areaId=${areaId}` : '') +
            (status !== 'all' ? `&status=${status}` : '') +
            `&page=${page}&pageSize=${pageSize}`,
        createTable: '/TableManage/CreateTable',
        updateTable: '/TableManage/UpdateTable',
        deleteTable: (id) => `/TableManage/DeleteTable?id=${id}`
    };

    // DOM Elements
    const DOM = {
        areaTable: document.getElementById("areaTable"),
        tableTable: document.getElementById("tableTable"),
        areaSearchInput: document.getElementById("areaSearchInput"),
        tableSearchInput: document.getElementById("tableSearchInput"),
        areaFilter: document.getElementById("areaFilter"),
        statusFilter: document.getElementById("statusFilter"),
        createAreaBtn: document.getElementById("createAreaBtn"),
        createTableBtn: document.getElementById("createTableBtn"),
        areaModal: document.getElementById("areaModal"),
        tableModal: document.getElementById("tableModal"),
        areaForm: document.getElementById("areaForm"),
        tableForm: document.getElementById("tableForm"),
        areaId: document.getElementById("areaId"),
        areaName: document.getElementById("areaName"),
        areaDescription: document.getElementById("areaDescription"),
        tableId: document.getElementById("tableId"),
        tableName: document.getElementById("tableName"),
        tableAreaId: document.getElementById("tableAreaId"),
        tableCapacity: document.getElementById("tableCapacity"),
        tableStatus: document.getElementById("tableStatus"),
        saveAreaBtn: document.getElementById("saveAreaBtn"),
        saveTableBtn: document.getElementById("saveTableBtn"),
        areaPagination: document.getElementById("areaPagination"),
        areaPaginationPrev: document.getElementById("areaPaginationPrev"),
        areaPaginationNext: document.getElementById("areaPaginationNext"),
        areaPaginationItems: document.getElementById("areaPaginationItems"),
        tablePagination: document.getElementById("tablePagination"),
        tablePaginationPrev: document.getElementById("tablePaginationPrev"),
        tablePaginationNext: document.getElementById("tablePaginationNext"),
        tablePaginationItems: document.getElementById("tablePaginationItems")
    };

    // Skeleton Loaders
    function showAreaSkeletons() {
        DOM.areaTable.innerHTML = Array(5).fill().map(() => `
            <tr>
                <td><div class="skeleton" style="width: 100px; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 200px; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 50px; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 100px; height: 20px;"></div></td>
            </tr>
        `).join('');
    }

    function showTableSkeletons() {
        DOM.tableTable.innerHTML = Array(5).fill().map(() => `
            <tr>
                <td><div class="skeleton" style="width: 100px; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 100px; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 50px; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 100px; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 100px; height: 20px;"></div></td>
            </tr>
        `).join('');
    }

    // Data Loading
    async function loadAreas(page = 1, searchTerm = state.areaSearchTerm) {
        try {
            showAreaSkeletons();

            state.areaPagination.currentPage = page;
            state.areaSearchTerm = searchTerm;

            const url = API.areas(searchTerm, page, state.areaPagination.pageSize);
            const result = await utils.fetchData(url);

            if (result.success) {
                state.areas = result.data;
                state.areaPagination = result.pagination;
                renderAreas();
                renderAreaPagination();
                updateAreaFilterOptions();
            } else {
                utils.showToast(result.message || "Không thể tải danh sách khu vực", "error");
            }
        } catch (error) {
            console.error('Load areas error:', error);
            utils.showToast("Không thể tải danh sách khu vực", "error");
        }
    }

    async function loadTables(page = 1, searchTerm = state.tableSearchTerm, areaId = state.tableAreaFilter, status = state.tableStatusFilter) {
        try {
            showTableSkeletons();

            state.tablePagination.currentPage = page;
            state.tableSearchTerm = searchTerm;
            state.tableAreaFilter = areaId;
            state.tableStatusFilter = status;

            const url = API.tables(searchTerm, areaId, status, page, state.tablePagination.pageSize);
            const result = await utils.fetchData(url);

            if (result.success) {
                state.tables = result.data;
                state.tablePagination = result.pagination;
                renderTables();
                renderTablePagination();
            } else {
                utils.showToast(result.message || "Không thể tải danh sách bàn", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        }
    }

    // Rendering Functions
    function renderAreas() {
        DOM.areaTable.innerHTML = state.areas.length === 0
            ? `<tr><td colspan="4" class="text-center">Không tìm thấy khu vực</td></tr>`
            : state.areas.map(area => `
                <tr data-area-id="${area.areaID}">
                    <td>${area.areaName}</td>
                    <td>${area.description || '-'}</td>
                    <td>${area.tableCount}</td>
                    <td>
                        <button class="btn btn-sm btn-outline-primary edit-area-btn" data-area-id="${area.areaID}">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger delete-area-btn" data-area-id="${area.areaID}">
                            <i class="fas fa-trash"></i>
                        </button>
                    </td>
                </tr>
            `).join('');
    }

    function renderTables() {
        DOM.tableTable.innerHTML = state.tables.length === 0
            ? `<tr><td colspan="5" class="text-center">Không tìm thấy bàn</td></tr>`
            : state.tables.map(table => `
                <tr data-table-id="${table.tableID}">
                    <td>${table.tableName}</td>
                    <td>${table.areaName}</td>
                    <td>${table.capacity}</td>
                    <td>
                        <span class="badge ${table.status === 0 ? 'bg-success' : table.status === 1 ? 'bg-warning' : 'bg-danger'}">
                            ${table.statusText}
                        </span>
                    </td>
                    <td>
                        <button class="btn btn-sm btn-outline-primary edit-table-btn" data-table-id="${table.tableID}">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger delete-table-btn" data-table-id="${table.tableID}">
                            <i class="fas fa-trash"></i>
                        </button>
                    </td>
                </tr>
            `).join('');
    }

    function updateAreaFilterOptions() {
        DOM.tableAreaId.innerHTML = `<option value="0" selected>Không có khu vực</option>` +
            state.areas.map(area => `
                <option value="${area.areaID}">${area.areaName}</option>
            `).join('');
        DOM.areaFilter.innerHTML = `<option value="0" selected>Tất cả khu vực</option>` +
            state.areas.map(area => `
                <option value="${area.areaID}">${area.areaName}</option>
            `).join('');
    }

    function renderAreaPagination() {
        utils.renderPagination(
            state.areaPagination,
            DOM.areaPagination,
            DOM.areaPaginationItems,
            DOM.areaPaginationPrev,
            DOM.areaPaginationNext,
            (pageNum) => loadAreas(pageNum, state.areaSearchTerm)
        );
    }

    function renderTablePagination() {
        utils.renderPagination(
            state.tablePagination,
            DOM.tablePagination,
            DOM.tablePaginationItems,
            DOM.tablePaginationPrev,
            DOM.tablePaginationNext,
            (pageNum) => loadTables(pageNum, state.tableSearchTerm, state.tableAreaFilter, state.tableStatusFilter)
        );
    }

    // Form Handling
    function resetAreaForm() {
        DOM.areaId.value = "0";
        DOM.areaName.value = "";
        DOM.areaDescription.value = "";
        DOM.areaModal.querySelector(".modal-title").textContent = "Thêm khu vực";
    }

    function resetTableForm() {
        DOM.tableId.value = "0";
        DOM.tableName.value = "";
        DOM.tableAreaId.value = "0";
        DOM.tableCapacity.value = "";
        DOM.tableStatus.value = "0";
        DOM.tableModal.querySelector(".modal-title").textContent = "Thêm bàn";
    }

    function populateAreaForm(area) {
        DOM.areaId.value = area.areaID;
        DOM.areaName.value = area.areaName;
        DOM.areaDescription.value = area.description || "";
        DOM.areaModal.querySelector(".modal-title").textContent = "Chỉnh sửa khu vực";
    }

    function populateTableForm(table) {
        DOM.tableId.value = table.tableID;
        DOM.tableName.value = table.tableName;
        DOM.tableAreaId.value = table.areaID || "0";
        DOM.tableCapacity.value = table.capacity;
        DOM.tableStatus.value = table.status;
        DOM.tableModal.querySelector(".modal-title").textContent = "Chỉnh sửa bàn";
    }

    // CRUD Operations
    async function saveArea() {
        const areaData = {
            AreaID: parseInt(DOM.areaId.value) || 0,
            AreaName: DOM.areaName.value.trim(),
            Description: DOM.areaDescription.value.trim() || null
        };

        if (!areaData.AreaName) {
            utils.showToast("Tên khu vực là bắt buộc", "error");
            return;
        }

        try {
            utils.showLoadingOverlay(true);
            const isUpdate = areaData.AreaID > 0;
            const url = isUpdate ? API.updateArea : API.createArea;
            const method = isUpdate ? 'PUT' : 'POST';

            const result = await utils.fetchData(url, method, areaData);

            if (result.success) {
                utils.showToast(isUpdate ? "Cập nhật khu vực thành công" : "Tạo khu vực thành công", "success");
                bootstrap.Modal.getInstance(DOM.areaModal).hide();
                await loadAreas(state.areaPagination.currentPage);
            } else {
                utils.showToast(result.message || "Lỗi khi lưu khu vực", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function saveTable() {
        const tableData = {
            TableID: parseInt(DOM.tableId.value) || 0,
            TableName: DOM.tableName.value.trim(),
            AreaID: parseInt(DOM.tableAreaId.value) || null,
            Capacity: parseInt(DOM.tableCapacity.value),
            Status: parseInt(DOM.tableStatus.value)
        };

        if (!tableData.TableName) {
            utils.showToast("Tên bàn là bắt buộc", "error");
            return;
        }

        if (!tableData.Capacity || tableData.Capacity <= 0) {
            utils.showToast("Sức chứa phải là số dương", "error");
            return;
        }

        try {
            utils.showLoadingOverlay(true);
            const isUpdate = tableData.TableID > 0;
            const url = isUpdate ? API.updateTable : API.createTable;
            const method = isUpdate ? 'PUT' : 'POST';

            const result = await utils.fetchData(url, method, tableData);

            if (result.success) {
                utils.showToast(isUpdate ? "Cập nhật bàn thành công" : "Tạo bàn thành công", "success");
                bootstrap.Modal.getInstance(DOM.tableModal).hide();
                await loadTables(state.tablePagination.currentPage);
            } else {
                utils.showToast(result.message || "Lỗi khi lưu bàn", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function deleteArea(areaId) {
        if (!confirm("Bạn có chắc chắn muốn xóa khu vực này? Các bàn liên quan sẽ được đặt thành không có khu vực.")) return;

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.deleteArea(areaId), 'DELETE');

            if (result.success) {
                utils.showToast("Xóa khu vực thành công", "success");
                await loadAreas(state.areaPagination.currentPage);
            } else {
                utils.showToast(result.message || "Lỗi khi xóa khu vực", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function deleteTable(tableId) {
        if (!confirm("Bạn có chắc chắn muốn xóa bàn này?")) return;

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.deleteTable(tableId), 'DELETE');

            if (result.success) {
                utils.showToast("Xóa bàn thành công", "success");
                await loadTables(state.tablePagination.currentPage);
            } else {
                utils.showToast(result.message || "Lỗi khi xóa bàn", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Event Listeners
    function setupEventListeners() {
        let areaSearchTimeout, tableSearchTimeout;

        DOM.areaSearchInput?.addEventListener("input", function () {
            clearTimeout(areaSearchTimeout);
            areaSearchTimeout = setTimeout(() => loadAreas(1, this.value.toLowerCase()), 300);
        });

        DOM.tableSearchInput?.addEventListener("input", function () {
            clearTimeout(tableSearchTimeout);
            tableSearchTimeout = setTimeout(() => loadTables(1, this.value.toLowerCase(), state.tableAreaFilter, state.tableStatusFilter), 300);
        });

        DOM.areaFilter?.addEventListener("change", function () {
            loadTables(1, state.tableSearchTerm, parseInt(this.value), state.tableStatusFilter);
        });

        DOM.statusFilter?.addEventListener("click", (e) => {
            const btn = e.target.closest("button[data-status]");
            if (btn) {
                DOM.statusFilter.querySelectorAll("button").forEach(b => b.classList.remove("active"));
                btn.classList.add("active");
                const status = btn.getAttribute("data-status");
                loadTables(1, state.tableSearchTerm, state.tableAreaFilter, status);
            }
        });

        DOM.createAreaBtn?.addEventListener("click", resetAreaForm);

        DOM.createTableBtn?.addEventListener("click", resetTableForm);

        DOM.saveAreaBtn?.addEventListener("click", saveArea);

        DOM.saveTableBtn?.addEventListener("click", saveTable);

        document.addEventListener("click", (e) => {
            const editAreaBtn = e.target.closest(".edit-area-btn");
            const deleteAreaBtn = e.target.closest(".delete-area-btn");
            const editTableBtn = e.target.closest(".edit-table-btn");
            const deleteTableBtn = e.target.closest(".delete-table-btn");

            if (editAreaBtn) {
                const areaId = parseInt(editAreaBtn.getAttribute("data-area-id"));
                const area = state.areas.find(a => a.areaID === areaId);
                if (area) {
                    populateAreaForm(area);
                    bootstrap.Modal.getOrCreateInstance(DOM.areaModal).show();
                }
            }

            if (deleteAreaBtn) {
                const areaId = parseInt(deleteAreaBtn.getAttribute("data-area-id"));
                deleteArea(areaId);
            }

            if (editTableBtn) {
                const tableId = parseInt(editTableBtn.getAttribute("data-table-id"));
                const table = state.tables.find(t => t.tableID === tableId);
                if (table) {
                    populateTableForm(table);
                    bootstrap.Modal.getOrCreateInstance(DOM.tableModal).show();
                }
            }

            if (deleteTableBtn) {
                const tableId = parseInt(deleteTableBtn.getAttribute("data-table-id"));
                deleteTable(tableId);
            }
        });

        DOM.areaModal?.addEventListener("hidden.bs.modal", resetAreaForm);
        DOM.tableModal?.addEventListener("hidden.bs.modal", resetTableForm);
    }

    // Initialize
    async function initialize() {
        try {
            await Promise.all([
                loadAreas(),
                loadTables()
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