document.addEventListener("DOMContentLoaded", () => {
    const state = {
        users: [],
        roles: [],
        permissions: [],
        rolePermissions: {},
        pagination: {
            currentPage: 1,
            pageSize: 8,
            totalItems: 0,
            totalPages: 0
        },
        searchTerm: "",
        statusFilter: "all"
    };

    // API endpoints
    const API = {
        users: (page, pageSize, searchTerm, statusFilter) => 
            `/UserManage/GetUsers?page=${page}&pageSize=${pageSize}${searchTerm ? `&searchTerm=${encodeURIComponent(searchTerm)}` : ''}${statusFilter !== 'all' ? `&status=${statusFilter}` : ''}`,
        roles: '/UserManage/GetRoles',
        permissions: '/UserManage/GetPermissions',
        rolePermissions: (id) => `/UserManage/GetRolePermissions?roleId=${id}`,
        createUser: '/UserManage/CreateUser',
        updateUser: '/UserManage/UpdateUser',
        deleteUser: (id) => `/UserManage/DeleteUser?id=${id}`,
        createRole: '/UserManage/CreateRole',
        updateRole: '/UserManage/UpdateRole',
        deleteRole: (id) => `/UserManage/DeleteRole?id=${id}`,
        updateRolePermissions: '/UserManage/UpdateRolePermissions'
    };

    // DOM elements
    const DOM = {
        userCardsRow: document.getElementById("userCardsRow"),
        statusFilter: document.getElementById("statusFilter"),
        rolesTableBody: document.getElementById("rolesTableBody"),
        permissionsTableBody: document.getElementById("permissionsTableBody"),
        roleSelect: document.getElementById("roleSelect"),
        userRoleSelect: document.getElementById("userRole"),
        editUserRoleSelect: document.getElementById("editUserRole"),
        userSearchInput: document.getElementById("userSearchInput"),
        roleSearchInput: document.getElementById("roleSearchInput"),
        saveUserBtn: document.getElementById("saveUserBtn"),
        saveEditUserBtn: document.getElementById("saveEditUserBtn"),
        saveRoleBtn: document.getElementById("saveRoleBtn"),
        saveEditRoleBtn: document.getElementById("saveEditRoleBtn"),
        savePermissionsBtn: document.getElementById("savePermissionsBtn"),
        refreshPermissionsBtn: document.getElementById("refreshPermissionsBtn"),
        paginationContainer: document.getElementById("userPagination"),
        paginationPrev: document.getElementById("paginationPrev"),
        paginationNext: document.getElementById("paginationNext"),
        paginationItems: document.getElementById("paginationItems"),
    };

    // Initialize
    async function initialize() {
        try {
            await Promise.all([
                loadUsers(1),
                loadRoles(),
                loadPermissions()
            ]);
            setupEventListeners();
            if (state.roles.length > 0) {
                await loadPermissionsForRole(state.roles[0].roleID);
            }
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi tải dữ liệu", "error");
        }
    }

    // Skeleton loaders
    function showUserCardSkeletons() {
        DOM.userCardsRow.innerHTML = Array(8).fill().map(() => `
            <div class="col-md-6 col-lg-3 mb-4">
                <div class="user-card-skeleton">
                    <div class="user-card-header">
                        <div class="user-avatar-skeleton skeleton"></div>
                        <div class="user-name-skeleton skeleton"></div>
                        <div class="user-role-skeleton skeleton"></div>
                    </div>
                    <div class="user-card-body">
                        <div class="user-info-skeleton skeleton"></div>
                        <div class="user-info-skeleton skeleton"></div>
                        <div class="user-info-skeleton skeleton"></div>
                    </div>
                    <div class="user-card-footer-skeleton">
                        <div class="btn-skeleton skeleton"></div>
                        <div class="btn-skeleton skeleton"></div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    function showRoleTableSkeletons() {
        DOM.rolesTableBody.innerHTML = Array(5).fill().map(() => `
            <tr>
                <td><div class="skeleton" style="height: 24px; width: 30px;"></div></td>
                <td><div class="skeleton" style="height: 24px; width: 180px;"></div></td>
                <td><div class="skeleton" style="height: 24px; width: 280px;"></div></td>
                <td><div class="skeleton" style="height: 24px; width: 100px;"></div></td>
                <td>
                    <div class="d-flex justify-content-center">
                        <div class="skeleton" style="height: 32px; width: 40px; margin-right: 10px;"></div>
                        <div class="skeleton" style="height: 32px; width: 40px;"></div>
                    </div>
                </td>
            </tr>
        `).join('');
    }

    function showPermissionTableSkeletons() {
        DOM.permissionsTableBody.innerHTML = Array(6).fill().map(() => `
            <tr>
                <td><div class="skeleton" style="height: 24px; width: 90%;"></div></td>
                <td><div class="skeleton" style="height: 24px; width: 24px; margin: 0 auto;"></div></td>
                <td><div class="skeleton" style="height: 24px; width: 24px; margin: 0 auto;"></div></td>
                <td><div class="skeleton" style="height: 24px; width: 24px; margin: 0 auto;"></div></td>
                <td><div class="skeleton" style="height: 24px; width: 24px; margin: 0 auto;"></div></td>
            </tr>
        `).join('');
    }

    // Data loading
    async function loadUsers(page = 1, searchTerm = state.searchTerm, statusFilter = state.statusFilter) {
        try {
            showUserCardSkeletons();
            state.pagination.currentPage = page;
            state.searchTerm = searchTerm;
            state.statusFilter = statusFilter;
            const result = await utils.fetchData(API.users(page, state.pagination.pageSize, searchTerm, statusFilter));
            if (result.success) {
                state.users = result.data;
                state.pagination = result.pagination;
                renderUsers(state.users);
                renderPagination();
            }
        } catch (error) {
            utils.showToast("Không thể tải danh sách người dùng", "error");
        } 
    }

    async function loadRoles() {
        try {
            showRoleTableSkeletons();
            const result = await utils.fetchData(API.roles);
            if (result.success) {
                state.roles = result.data;
                renderRoles(state.roles);
                populateRoleSelects();
            }
        } catch (error) {
            utils.showToast("Không thể tải danh sách vai trò", "error");
        } 
    }

    async function loadPermissions() {
        try {
            showPermissionTableSkeletons();
            const result = await utils.fetchData(API.permissions);
            if (result.success) {
                state.permissions = result.data;
            }
        } catch (error) {
            utils.showToast("Không thể tải danh sách quyền", "error");
        } 
    }

    async function loadPermissionsForRole(roleId) {
        try {
            showPermissionTableSkeletons();
            const result = await utils.fetchData(API.rolePermissions(roleId));
            if (result.success) {
                state.rolePermissions[roleId] = result.data;
                renderPermissionsTable(roleId);
            }
        } catch (error) {
            utils.showToast("Không thể tải thông tin phân quyền", "error");
        }
    }

    // Rendering
    function renderUsers(usersData) {
        DOM.userCardsRow.innerHTML = usersData.length === 0 ? `
            <div class="col-12">
                <div class="alert alert-info">
                    Không có người dùng nào. Hãy thêm người dùng mới.
                </div>
            </div>
        ` : usersData.map(user => {
            const roleName = user.role ? user.role.roleName : "Chưa phân quyền";
            const statusText = utils.getStatusText(user.status);
            const statusClass = { 0: "status-active", 1: "status-inactive", 2: "status-left" }[user.status] || "status-unknown";
            return `
                <div class="col-md-6 col-lg-3">
                    <div class="user-card" data-user-id="${user.userID}">
                        <div class="user-card-header">
                            <div class="user-avatar-large">${utils.getInitials(user.fullName)}</div>
                            <h3 class="user-name">${user.fullName}</h3>
                            <p class="user-role">${roleName}</p>
                        </div>
                        <div class="user-card-body">
                            <div class="user-info-item">
                                <div class="user-info-label">Email:</div>
                                <div class="user-info-value">${user.email || "N/A"}</div>
                            </div>
                            <div class="user-info-item">
                                <div class="user-info-label">Điện thoại:</div>
                                <div class="user-info-value">${user.phoneNumber || "N/A"}</div>
                            </div>
                            <div class="user-info-item">
                                <div class="user-info-label">Lương</div>
                                <div class="user-info-value">
                                    <span class="user-salary">${user.salary ? utils.formatMoney(user.salary) : "N/A"}</span>
                                </div>
                            </div>
                            <div class="user-info-item">
                                <div class="user-info-label">Trạng thái:</div>
                                <div class="user-info-value">
                                    <span class="user-status ${statusClass}">${statusText}</span>
                                </div>
                            </div>
                        </div>
                        <div class="user-card-footer">
                            <button class="btn btn-edit edit-user-btn" data-user-id="${user.userID}">
                                <i class="fas fa-edit me-2"></i> Sửa
                            </button>
                            <button class="btn btn-delete delete-user-btn" data-user-id="${user.userID}" data-user-name="${user.fullName}">
                                <i class="fas fa-trash-alt me-2"></i> Xóa
                            </button>
                        </div>
                    </div>
                </div>
            `;
        }).join('');
    }

    function renderRoles(rolesData) {
        DOM.rolesTableBody.innerHTML = rolesData.length === 0 ? `
            <tr><td colspan="5" class="text-center">Không có vai trò nào. Hãy thêm vai trò mới.</td></tr>
        ` : rolesData.map(role => `
            <tr>
                <td>${role.roleID}</td>
                <td>
                    <div class="role-name">${role.roleName}</div>
                    <div class="role-key text-muted small">${role.roleKey}</div>
                </td>
                <td>${role.description || "Không có mô tả"}</td>
                <td>${role.userCount || 0}</td>
                <td class="text-center">
                    <button class="btn btn-sm btn-outline-primary edit-role-btn me-2" data-role-id="${role.roleID}">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="btn btn-sm btn-outline-danger delete-role-btn" data-role-id="${role.roleID}" data-role-name="${role.roleName}">
                        <i class="fas fa-trash-alt"></i>
                    </button>
                </td>
            </tr>
        `).join('');
    }

    function renderPermissionsTable(roleId) {
        DOM.permissionsTableBody.innerHTML = state.permissions.length === 0 ? `
            <tr><td colspan="5" class="text-center">Không có quyền nào được định nghĩa.</td></tr>
        ` : state.permissions.map(permission => {
            const rolePerm = (state.rolePermissions[roleId] || []).find(rp => rp.permissionID === permission.permissionID) || {
                canView: false,
                canAdd: false,
                canEdit: false,
                canDelete: false
            };
            return `
                <tr>
                    <td>
                        <div class="permission-name">${permission.permissionName}</div>
                        <div class="permission-key text-muted small">${permission.permissionKey}</div>
                    </td>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input permission-checkbox" 
                               data-permission-id="${permission.permissionID}" 
                               data-permission-type="view" ${rolePerm.canView ? "checked" : ""}>
                    </td>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input permission-checkbox" 
                               data-permission-id="${permission.permissionID}" 
                               data-permission-type="add" ${rolePerm.canAdd ? "checked" : ""}>
                    </td>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input permission-checkbox" 
                               data-permission-id="${permission.permissionID}" 
                               data-permission-type="edit" ${rolePerm.canEdit ? "checked" : ""}>
                    </td>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input permission-checkbox" 
                               data-permission-id="${permission.permissionID}" 
                               data-permission-type="delete" ${rolePerm.canDelete ? "checked" : ""}>
                    </td>
                </tr>
            `;
        }).join('');
    }

    function populateRoleSelects() {
        const selects = [DOM.roleSelect, DOM.userRoleSelect, DOM.editUserRoleSelect].filter(Boolean);
        selects.forEach(select => select.innerHTML = state.roles.length === 0 ? 
            '<option value="">Không có vai trò nào</option>' : 
            state.roles.map(role => `<option value="${role.roleID}">${role.roleName}</option>`).join('')
        );

        DOM.roleSelect?.addEventListener("change", () => loadPermissionsForRole(parseInt(DOM.roleSelect.value)));
    }

    function renderPagination() {
        utils.renderPagination(
            state.pagination,
            DOM.paginationContainer,
            DOM.paginationItems,
            DOM.paginationPrev,
            DOM.paginationNext,
            (pageNum) => loadUsers(pageNum, state.searchTerm, state.statusFilter)
        );
    }

    // Event listeners
    function setupEventListeners() {
        let searchTimeout;
        DOM.userSearchInput?.addEventListener("input", function() {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => loadUsers(state.pagination.currentPage, this.value.toLowerCase(), state.statusFilter), 300);
        });

        DOM.statusFilter?.addEventListener("change", () => loadUsers(state.pagination.currentPage, state.searchTerm, DOM.statusFilter.value));

        DOM.roleSearchInput?.addEventListener("input", function() {
            const filteredRoles = state.roles.filter(role => 
                role.roleName.toLowerCase().includes(this.value.toLowerCase()) || 
                (role.description && role.description.toLowerCase().includes(this.value.toLowerCase()))
            );
            renderRoles(filteredRoles);
        });

        DOM.saveUserBtn?.addEventListener("click", createUser);
        DOM.saveEditUserBtn?.addEventListener("click", updateUser);
        DOM.saveRoleBtn?.addEventListener("click", createRole);
        DOM.saveEditRoleBtn?.addEventListener("click", updateRole);
        DOM.savePermissionsBtn?.addEventListener("click", updateRolePermissions);
        DOM.refreshPermissionsBtn?.addEventListener("click", () => loadPermissionsForRole(parseInt(DOM.roleSelect.value)));

        DOM.paginationPrev?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage > 1) {
                loadUsers(state.pagination.currentPage - 1, state.searchTerm, state.statusFilter);
            }
        });

        DOM.paginationNext?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage < state.pagination.totalPages) {
                loadUsers(state.pagination.currentPage + 1, state.searchTerm, state.statusFilter);
            }
        });

        document.addEventListener("click", (e) => {
            const editBtn = e.target.closest(".edit-user-btn");
            const deleteBtn = e.target.closest(".delete-user-btn");
            const editRoleBtn = e.target.closest(".edit-role-btn");
            const deleteRoleBtn = e.target.closest(".delete-role-btn");

            if (editBtn) openEditUserModal(parseInt(editBtn.getAttribute("data-user-id")));
            if (deleteBtn) openDeleteUserModal(parseInt(deleteBtn.getAttribute("data-user-id")));
            if (editRoleBtn) openEditRoleModal(parseInt(editRoleBtn.getAttribute("data-role-id")));
            if (deleteRoleBtn) confirmDeleteRole(
                parseInt(deleteRoleBtn.getAttribute("data-role-id")),
                deleteRoleBtn.getAttribute("data-role-name")
            );
        });
    }

    // CRUD operations
    async function createUser() {
        const userData = {
            fullName: document.getElementById("userFullName").value.trim(),
            email: document.getElementById("userEmail").value.trim(),
            phoneNumber: document.getElementById("userPhone").value.trim(),
            passwordHash: document.getElementById("userPassword").value.trim(),
            salary: document.getElementById("userSalary").value.trim() === "" ? "" : Number(document.getElementById("userSalary").value.trim()),
            address: document.getElementById("userAddress").value.trim(),
            roleID: parseInt(document.getElementById("userRole").value) || null,
            userType: 1,
            status: parseInt(document.getElementById("userStatus").value)
        };

        const errors = validateUser(userData);
        if (errors.length > 0) {
            utils.showToast(errors.join("<br>"), "error");
            return;
        }

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.createUser, 'POST', userData);
            if (result.success) {
                utils.showToast("Thêm người dùng thành công", "success");
                await loadUsers(state.pagination.currentPage, state.searchTerm, state.statusFilter);
                await loadRoles();
                document.getElementById("addUserForm").reset();
                bootstrap.Modal.getInstance(document.getElementById('addUserModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi thêm người dùng", "error");
            }
        } catch (error) {
            utils.showToast("Đã xảy ra lỗi khi thêm người dùng", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function updateUser() {
        const userData = {
            userID: parseInt(document.getElementById("editUserId").value),
            fullName: document.getElementById("editFullName").value.trim(),
            email: document.getElementById("editUserEmail").value.trim(),
            salary: document.getElementById("editUserSalary").value.trim() === "" ? "" : Number(document.getElementById("editUserSalary").value.trim()),
            address: document.getElementById("editUserAddress").value.trim(),
            phoneNumber: document.getElementById("editUserPhone").value.trim(),
            roleID: parseInt(document.getElementById("editUserRole").value) || null,
            status: parseInt(document.getElementById("editUserStatus").value)
        };

        const errors = validateUser(userData);
        if (errors.length > 0) {
            utils.showToast(errors.join("<br>"), "error");
            return;
        }

        const password = document.getElementById("editUserPassword").value.trim();
        const updateModel = {
            user: userData,
            updatePassword: password.length > 0,
            newPassword: password.length > 0 ? password : null
        };

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.updateUser, 'POST', updateModel);
            if (result.success) {
                utils.showToast("Cập nhật người dùng thành công", "success");
                await loadUsers(state.pagination.currentPage, state.searchTerm, state.statusFilter);
                bootstrap.Modal.getInstance(document.getElementById('editUserModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi cập nhật người dùng", "error");
            }
        } catch (error) {
            utils.showToast("Đã xảy ra lỗi khi cập nhật người dùng", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function openDeleteUserModal(userId) {
        const user = state.users.find(u => u.userID === userId);
        if (!user) return;
        console.log("User to delete:", user.fullName);
        document.getElementById("deleteFullName").innerHTML = user.fullName;
        document.getElementById("deleteUserId").value = user.userID;
        const deleteModal = new bootstrap.Modal(document.getElementById("deleteUserModal"));
        deleteModal.show();

        const confirmBtn = document.getElementById("confirmDeleteUserBtn");
        const deleteHandler = async () => {
            try {
                utils.showLoadingOverlay(true);
                const result = await utils.fetchData(API.deleteUser(userId), 'POST');
                if (result.success) {
                    utils.showToast("Xóa người dùng thành công", "success");
                    await loadUsers(state.pagination.currentPage, state.searchTerm, state.statusFilter);
                    await loadRoles();
                    deleteModal.hide();
                } else {
                    utils.showToast(result.message || "Lỗi khi xóa người dùng", "error");
                }
            } catch (error) {
                utils.showToast("Đã xảy ra lỗi khi xóa người dùng", "error");
            }finally {
                utils.showLoadingOverlay(false);
            }
            confirmBtn.removeEventListener("click", deleteHandler);
        };
        confirmBtn.addEventListener("click", deleteHandler);
    }

    function openEditUserModal(userId) {
        const user = state.users.find(u => u.userID === userId);
        if (!user) return;

        document.getElementById("editUserId").value = user.userID;
        document.getElementById("editFullName").value = user.fullName;
        document.getElementById("editUserEmail").value = user.email || "";
        document.getElementById("editUserPhone").value = user.phoneNumber || "";
        document.getElementById("editUserRole").value = user.roleID || "";
        document.getElementById("editUserStatus").value = user.status || 0;
        document.getElementById("editUserPassword").value = "";
        document.getElementById("editUserSalary").value = user.salary !== undefined && user.salary !== null ? user.salary : "";
        document.getElementById("editUserAddress").value = user.address || "";

        new bootstrap.Modal(document.getElementById("editUserModal")).show();
    }

    async function createRole() {
        const roleData = {
            roleKey: document.getElementById("roleKey").value.trim(),
            roleName: document.getElementById("roleName").value.trim(),
            description: document.getElementById("roleDescription").value.trim()
        };

        const errors = validateRole(roleData);
        if (errors.length > 0) {
            utils.showToast(errors.join("<br>"), "error");
            return;
        }
        roleData.roleKey = roleData.roleKey.toUpperCase().replace(/\s+/g, '_');
        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.createRole, 'POST', roleData);
            if (result.success) {
                utils.showToast("Thêm vai trò thành công", "success");
                await loadRoles();
                document.getElementById("addRoleForm").reset();
                bootstrap.Modal.getInstance(document.getElementById('addRoleModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi thêm vai trò", "error");
            }
        } catch (error) {
            utils.showToast("Đã xảy ra lỗi khi thêm vai trò", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function updateRole() {
        const roleData = {
            roleID: parseInt(document.getElementById("editRoleId").value),
            roleKey: document.getElementById("editRoleKey").value.trim(),
            roleName: document.getElementById("editRoleName").value.trim(),
            description: document.getElementById("editRoleDescription").value.trim()
        };

        const errors = validateRole(roleData);
        if (errors.length > 0) {
            utils.showToast(errors.join("<br>"), "error");
            return;
        }
        roleData.roleKey = roleData.roleKey.toUpperCase().replace(/\s+/g, '_');

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.updateRole, 'POST', roleData);
            if (result.success) {
                utils.showToast("Cập nhật vai trò thành công", "success");
                await loadRoles();
                bootstrap.Modal.getInstance(document.getElementById('editRoleModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi cập nhật vai trò", "error");
            }
        } catch (error) {
            utils.showToast("Đã xảy ra lỗi khi cập nhật vai trò", "error");
        }finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function confirmDeleteRole(roleId, roleName) {
        if (!confirm(`Bạn có chắc chắn muốn xóa vai trò "${roleName}"?`)) return;

        try {
            
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.deleteRole(roleId), 'POST');
            if (result.success) {
                utils.showToast("Xóa vai trò thành công", "success");
                await loadRoles();
            } else {
                utils.showToast(result.message || "Lỗi khi xóa vai trò", "error");
            }
        } catch (error) {
            utils.showToast("Đã xảy ra lỗi khi xóa vai trò", "error");
        }finally {
            utils.showLoadingOverlay(false);
        }
    }

    function openEditRoleModal(roleId) {
        const role = state.roles.find(r => r.roleID === roleId);
        if (!role) return;

        document.getElementById("editRoleId").value = role.roleID;
        document.getElementById("editRoleKey").value = role.roleKey;
        document.getElementById("editRoleName").value = role.roleName;
        document.getElementById("editRoleDescription").value = role.description || "";
        new bootstrap.Modal(document.getElementById("editRoleModal")).show();
    }

    async function updateRolePermissions() {
        const roleId = parseInt(DOM.roleSelect.value);
        const permissionGroups = Array.from(document.querySelectorAll(".permission-checkbox")).reduce((groups, checkbox) => {
            const permissionId = parseInt(checkbox.getAttribute("data-permission-id"));
            const permissionType = checkbox.getAttribute("data-permission-type");
            groups[permissionId] = groups[permissionId] || {
                permissionID: permissionId,
                canView: false,
                canAdd: false,
                canEdit: false,
                canDelete: false
            };
            groups[permissionId][`can${permissionType.charAt(0).toUpperCase() + permissionType.slice(1)}`] = checkbox.checked;
            return groups;
        }, {});

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.updateRolePermissions, 'POST', {
                roleID: roleId,
                permissions: Object.values(permissionGroups)
            });
            if (result.success) {
                utils.showToast("Cập nhật quyền thành công", "success");
                await loadPermissionsForRole(roleId);
            } else {
                utils.showToast(result.message || "Lỗi khi cập nhật quyền", "error");
            }
        } catch (error) {
            utils.showToast("Đã xảy ra lỗi khi cập nhật quyền", "error");
        }finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Validation
    function validateUser(userData) {
        const errors = [];

        if (!userData.fullName || userData.fullName.trim() === '') errors.push("Họ tên không được để trống");
        else if (userData.fullName.length > 100) errors.push("Họ tên không được vượt quá 100 ký tự");

        if (!userData.phoneNumber || userData.phoneNumber.trim() === '') errors.push("Số điện thoại không được để trống");
        else if (!/^[0-9]{10,15}$/.test(userData.phoneNumber)) errors.push("Số điện thoại phải có 10-15 chữ số");

        if (userData.email && userData.email.trim() !== '' && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(userData.email)) {
            errors.push("Email không đúng định dạng");
        }

        if (!userData.userID && (!userData.passwordHash || userData.passwordHash.trim() === '')) {
            errors.push("Mật khẩu không được để trống");
        } else if (!userData.userID && userData.passwordHash.length < 6) {
            errors.push("Mật khẩu phải có ít nhất 6 ký tự");
        }

        if (userData.address && userData.address.length > 200) errors.push("Địa chỉ không được vượt quá 200 ký tự");

        if (userData.salary === "" || userData.salary === undefined || userData.salary === null) {
            errors.push("Lương không được để trống và phải là số");
        } else if (isNaN(userData.salary)) {
            errors.push("Lương phải là số hợp lệ");
        } else if (Number(userData.salary) < 0) {
            errors.push("Lương không được nhỏ hơn 0");
        }

        return errors;
    }

    function validateRole(roleData) {
        const errors = [];
        if (!roleData.roleKey || roleData.roleKey.trim() === '') errors.push("Key vai trò không được để trống");
        if (!roleData.roleName || roleData.roleName.trim() === '') errors.push("Tên vai trò không được để trống");
        else if (roleData.roleName.length > 50) errors.push("Tên vai trò không được vượt quá 50 ký tự");

        if (roleData.description && roleData.description.length > 255) errors.push("Mô tả không được vượt quá 255 ký tự");
        return errors;
    }

    initialize();
});