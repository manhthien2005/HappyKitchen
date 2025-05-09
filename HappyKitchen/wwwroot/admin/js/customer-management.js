document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        users: [],
        pagination: {
            currentPage: 1,
            pageSize: 8,
            totalItems: 0,
            totalPages: 0
        },
        orderHistory: {
            orders: [],
            pagination: {
                currentPage: 1,
                pageSize: 5,
                totalItems: 0,
                totalPages: 0
            },
            customerId: null,
            customerName: "",
            customerPhone: ""
        },
        searchTerm: "",
        statusFilter: "all",
        sortFilter: "name_asc"
    };

    // API Endpoints
    const API = {
        users: (page, pageSize, searchTerm, statusFilter, sortFilter) =>
            `/CustomerManage/GetUsers?page=${page}&pageSize=${pageSize}`
            + (searchTerm ? `&searchTerm=${encodeURIComponent(searchTerm)}` : '')
            + (statusFilter !== 'all' ? `&status=${statusFilter}` : '')
            + (sortFilter !== 'name_asc' ? `&sortBy=${sortFilter}` : ''),
        createUser: '/CustomerManage/CreateUser',
        updateUser: '/CustomerManage/UpdateUser',
        deleteUser: (id) => `/CustomerManage/DeleteUser?id=${id}`,
        getUserOrders: (id, page = 1, pageSize = 5) => `/CustomerManage/GetUserOrders?userId=${id}&page=${page}&pageSize=${pageSize}`
    };

    // DOM Elements
    const DOM = {
        userCardsRow: document.getElementById("customerCardsRow"),
        statusFilter: document.getElementById("statusFilter"),
        sortFilter: document.getElementById("sortFilter"),
        userSearchInput: document.getElementById("customerSearchInput"),
        saveUserBtn: document.getElementById("saveCustomerBtn"),
        saveEditUserBtn: document.getElementById("updateCustomerBtn"),
        paginationContainer: document.getElementById("userPagination"),
        paginationPrev: document.getElementById("paginationPrev"),
        paginationNext: document.getElementById("paginationNext"),
        paginationItems: document.getElementById("paginationItems"),
        orderHistoryModal: document.getElementById("orderHistoryModal"),
        orderHistoryCustomerName: document.getElementById("orderHistoryCustomerName"),
        orderHistoryCustomerPhone: document.getElementById("orderHistoryCustomerPhone"),
        customerOrdersTableBody: document.getElementById("customerOrdersTableBody"),
        orderPaginationContainer: document.getElementById("orderHistoryPagination"),
        orderPaginationPrev: document.getElementById("orderPaginationPrev"),
        orderPaginationNext: document.getElementById("orderPaginationNext"),
        orderPaginationItems: document.getElementById("orderPaginationItems")
    };

    

    // Skeleton Loaders
    function showUserCardSkeletons() {
        DOM.userCardsRow.innerHTML = Array(8).fill().map(() => `
        <div class="col-md-6 col-lg-3">
          <div class="customer-card">
            <div class="customer-header">
              <div class="customer-avatar skeleton" style="width: 50px; height: 50px; border-radius: 50%;"></div>
              <div class="customer-info">
                <h3 class="customer-name skeleton" style="width: 80%; height: 24px;"></h3>
              </div>
            </div>
            <div class="customer-body">
              <div class="customer-contact">
                <div class="contact-item">
                  <i class="fas fa-phone"></i>
                  <span class="skeleton" style="width: 70%; height: 16px; display: inline-block;"></span>
                </div>
                <div class="contact-item">
                  <i class="fas fa-envelope"></i>
                  <span class="skeleton" style="width: 70%; height: 16px; display: inline-block;"></span>
                </div>
                <div class="contact-item">
                  <i class="fas fa-calendar"></i>
                  <span class="skeleton" style="width: 70%; height: 16px; display: inline-block;"></span>
                </div>
                <div class="status-item">
                  <i class="fas fa-hourglass-half"></i>
                  <span class="skeleton" style="width: 70%; height: 16px; display: inline-block;"></span>
                </div>
              </div>
              <div class="customer-stats">
                <div class="stat-item">
                  <span class="stat-value skeleton" style="width: 60%; height: 24px; display: block; margin-bottom: 8px;"></span>
                  <span class="stat-label skeleton" style="width: 80%; height: 16px; display: block;"></span>
                </div>
                <div class="stat-item">
                  <span class="stat-value skeleton" style="width: 60%; height: 24px; display: block; margin-bottom: 8px;"></span>
                  <span class="stat-label skeleton" style="width: 80%; height: 16px; display: block;"></span>
                </div>
              </div>
              <div class="customer-actions">
                <button class="btn btn-history skeleton" style="width: 30%; height: 38px; opacity: 0.7;"></button>
                <button class="btn btn-edit skeleton" style="width: 30%; height: 38px; opacity: 0.7;"></button>
                <button class="btn btn-delete skeleton" style="width: 30%; height: 38px; opacity: 0.7;"></button>
              </div>
            </div>
          </div>
        </div>
      `).join('');
    }

    // Data Loading
    async function loadUsers(page = 1, searchTerm = state.searchTerm, statusFilter = state.statusFilter, sortFilter = state.sortFilter) {
        try {
            showUserCardSkeletons();
            state.pagination.currentPage = page;
            state.searchTerm = searchTerm;
            state.statusFilter = statusFilter;
            state.sortFilter = sortFilter;

            const url = API.users(page, state.pagination.pageSize, searchTerm, statusFilter, sortFilter);
            const result = await utils.fetchData(url);

            if (result.success) {
                state.users = result.data;
                state.pagination = result.pagination;
                renderUsers(state.users);
                renderPagination();
            } else {
                utils.showToast(result.message || "Không thể tải danh sách người dùng", "error");
            }
        } catch (error) {
            console.error('Load users error:', error);
            utils.showToast("Không thể tải danh sách người dùng", "error");
        }
    }

    async function loadUserOrders(userId, userName, userPhone, page = 1) {
        try {
            utils.showLoadingOverlay(true);
            state.orderHistory.customerId = userId;
            state.orderHistory.customerPhone = userPhone;
            state.orderHistory.pagination.currentPage = page;

            const url = API.getUserOrders(userId, page, state.orderHistory.pagination.pageSize);
            const result = await utils.fetchData(url);
            console.log('API Response:', result); // Kiểm tra toàn bộ phản hồi
            console.log('Orders Data:', result.data); // Kiểm tra dữ liệu đơn hàng


            if (result.success) {
                const processedOrders = result.data.map(order => ({
                    ...order,
                    statusText: getOrderStatusText(order.status),
                    formattedDate: new Date(order.orderTime).toLocaleString('vi-VN')
                }));

                state.orderHistory.orders = processedOrders;
                state.orderHistory.pagination = result.pagination;

                DOM.orderHistoryCustomerName.textContent = `Khách hàng: ${userName || 'Không có tên'}`;
                DOM.orderHistoryCustomerPhone.textContent = `Số điện thoại: ${userPhone || 'Không có số điện thoại'}`;

                renderCustomerOrders(processedOrders);
                renderOrderPagination();

                // Đảm bảo đóng modal cũ nếu đang mở
                const existingBackdrops = document.querySelectorAll('.modal-backdrop');
                if (existingBackdrops.length > 0) {
                    existingBackdrops.forEach(backdrop => backdrop.remove());
                }

                // Đảm bảo modal hiện tại đã đóng trước khi mở lại
                const currentModal = bootstrap.Modal.getInstance(DOM.orderHistoryModal);
                if (currentModal) {
                    currentModal.dispose();
                }

                // Mở modal mới
                const orderHistoryModal = new bootstrap.Modal(DOM.orderHistoryModal);
                orderHistoryModal.show();
            } else {
                utils.showToast(result.message || "Không thể tải lịch sử đơn hàng", "error");
            }
        } catch (error) {
            console.error('Load orders error:', error);
            utils.showToast("Không thể tải lịch sử đơn hàng", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }


    // Rendering Functions
    function renderUsers(usersData) {
        DOM.userCardsRow.innerHTML = usersData.length === 0
            ? `
          <div class="col-12">
            <div class="alert alert-info">
              Không có người dùng nào. Hãy thêm người dùng mới.
            </div>
          </div>
        `
            : usersData.map(user => {
                const statusText = utils.getStatusText(user.status);
                const statusClass = { 0: "status-active", 1: "status-inactive", 2: "status-left" }[user.status] || "status-unknown";
                const date = new Date(user.lastOrderDate);
                let formatted = "Chưa mua hàng";
                if (user.lastOrderDate) {
                    const date = new Date(user.lastOrderDate);
                    formatted = `${date.getDate().toString().padStart(2, '0')}/${(date.getMonth() + 1).toString().padStart(2, '0')}/${date.getFullYear()} - ${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
                }
                const formattedSpent = user.totalSpent.toLocaleString('vi-VN', { maximumFractionDigits: 2 });
                return `
            <div class="col-md-6 col-lg-3">
              <div class="customer-card" data-customer-id="${user.userID || ''}">
                <div class="customer-header">
                  <div class="customer-avatar">${utils.getInitials(user.fullName || '')}</div>
                  <div class="customer-info">
                    <h3 class="customer-name">${user.fullName || 'Không có tên'}</h3>
                  </div>
                </div>
                <div class="customer-body">
                  <div class="customer-contact">
                    <div class="contact-item">
                      <i class="fas fa-phone"></i>
                      <span><b>SĐT: </b>${user.phoneNumber || 'Chưa cập nhật'}</span>
                    </div>
                    <div class="contact-item">
                      <i class="fas fa-envelope"></i>
                      <span><b>Email: </b>${user.email || 'Chưa cập nhật'}</span>
                    </div>
                    <div class="contact-item">
                      <i class="fas fa-calendar"></i>
                      <span><b>Đơn mua gần nhất: </b>${formatted}</span>
                    </div>
                    <div class="status-item">
                      <i class="fas fa-hourglass-half"></i>
                      <b style="margin-right: 3px;">Trạng thái: </b> <span class="user-status ${statusClass}">${statusText || 'Lỗi'}</span>
                    </div>
                  </div>
                  <div class="customer-stats">
                    <div class="stat-item">
                      <span class="stat-value">${user.totalOrders || 0}</span>
                      <span class="stat-label">Đơn hàng</span>
                    </div>
                    <div class="stat-item">
                      <span class="stat-value">${formattedSpent || 0}</span>
                      <span class="stat-label">Tổng chi tiêu</span>
                    </div>
                  </div>
                  <div class="customer-actions">
                    <button class="btn btn-history view-history-btn" data-customer-id="${user.userID || ''}" ${!user.userID ? 'disabled' : ''}>
                      <i class="fas fa-history me-2"></i> Lịch sử
                    </button>
                    <button class="btn btn-edit edit-customer-btn" data-customer-id="${user.userID || ''}" ${!user.userID ? 'disabled' : ''}>
                      <i class="fas fa-edit me-2"></i> Sửa
                    </button>
                    <button class="btn btn-delete delete-customer-btn" 
                      data-customer-id="${user.userID || ''}" 
                      data-customer-name="${user.fullName || ''}"
                      ${!user.userID ? 'disabled' : ''}>
                      <i class="fas fa-trash-alt me-2"></i> Xóa
                    </button>
                  </div>
                </div>
              </div>
            </div>
          `;
            }).join('');
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

    function renderOrderPagination() {
        utils.renderPagination(
            state.orderHistory.pagination,
            DOM.orderPaginationContainer,
            DOM.orderPaginationItems,
            DOM.orderPaginationPrev,
            DOM.orderPaginationNext,
            (pageNum) => loadUserOrders(
                state.orderHistory.customerId,
                state.orderHistory.customerName,
                state.orderHistory.customerPhone,
                pageNum
            )
        );
    }

    function renderCustomerOrders(orders) {
      const orderHistoryList = DOM.customerOrdersTableBody;
      orderHistoryList.innerHTML = "";

      if (!orders || orders.length === 0) {
          orderHistoryList.innerHTML = `
          <div class="empty-orders">
            <i class="fas fa-shopping-cart"></i>
            <p>Khách hàng chưa có đơn hàng nào</p>
          </div>
        `;
          return;
      }
      orders.forEach((order) => {
        const orderProducts = order.items || [];
        const orderElement = document.createElement("div");
        orderElement.className = "order-item";
  
        const orderHeader = document.createElement("div");
        orderHeader.className = "order-header";
        orderHeader.innerHTML = `
      <div class="d-flex align-items-center">
        <div class="order-id">#${order.orderID}</div>
        <div class="order-date ms-3">${order.formattedDate}</div>
      </div>
      <div class="order-status ${getOrderStatusClass(order.status)}">${order.statusText}</div>
    `;
          orderElement.appendChild(orderHeader);

          const orderBody = document.createElement("div");
          orderBody.className = "order-body";

          const productsContainer = document.createElement("div");
          productsContainer.className = "order-products";

          const productsHeader = document.createElement("div");
          productsHeader.className = "order-products-header";
          productsHeader.innerHTML = `
              <div class="product-name">Tên sản phẩm</div>
              <div class="product-unit-price">Đơn giá</div>
              <div class="product-quantity">Số lượng</div>
              <div class="product-price">Thành tiền</div>
            `;
          productsContainer.appendChild(productsHeader);

          const visibleProducts = orderProducts.slice(0, 2);
          visibleProducts.forEach((product) => {
              const productItem = document.createElement("div");
              productItem.className = "product-item";
              productItem.innerHTML = `
            <div class="product-name">${product.name}</div>
            <div class="product-unit-price">${utils.formatMoney(product.unitPrice || (product.price / product.quantity))}</div>
            <div class="product-quantity">x${product.quantity}</div>
            <div class="product-price">${utils.formatMoney(product.price)}</div>
          `;
              productsContainer.appendChild(productItem);
          });

          if (orderProducts.length > 2) {
              const remainingProducts = orderProducts.slice(2);
              const showMoreContainer = document.createElement("div");
              showMoreContainer.className = "show-more-container text-center mt-2";

              const showMoreBtn = document.createElement("button");
              showMoreBtn.className = "btn btn-link btn-sm show-more-btn";
              showMoreBtn.innerHTML = `<i class="fas fa-chevron-down me-1"></i>Xem thêm ${remainingProducts.length} sản phẩm`;

              const remainingProductsContainer = document.createElement("div");
              remainingProductsContainer.className = "remaining-products d-none";

              remainingProducts.forEach((product) => {
                  const productItem = document.createElement("div");
                  productItem.className = "product-item";
                  productItem.innerHTML = `
            <div class="product-name">${product.name}</div>
            <div class="product-unit-price">${utils.formatMoney(product.unitPrice || (product.price / product.quantity))}</div>
            <div class="product-quantity">x${product.quantity}</div>
            <div class="product-price">${utils.formatMoney(product.price)}</div>
          `;
                  remainingProductsContainer.appendChild(productItem);
              });

              showMoreBtn.addEventListener("click", () => {
                  remainingProductsContainer.classList.toggle("d-none");
                  const isExpanded = !remainingProductsContainer.classList.contains("d-none");
                  showMoreBtn.innerHTML = isExpanded
                      ? `<i class="fas fa-chevron-up me-1"></i>Ẩn bớt`
                      : `<i class="fas fa-chevron-down me-1"></i>Xem thêm ${remainingProducts.length} sản phẩm`;
              });

              showMoreContainer.appendChild(showMoreBtn);
              productsContainer.appendChild(remainingProductsContainer);
              productsContainer.appendChild(showMoreContainer);
          }

          orderBody.appendChild(productsContainer);
          orderElement.appendChild(orderBody);

          const orderFooter = document.createElement("div");
          orderFooter.className = "order-footer";
          orderFooter.innerHTML = `
            <div>
              <span class="order-total-label">Tổng tiền:</span>
              <span class="order-total-value">${utils.formatMoney(order.total)}</span>
            </div>
            <div class="order-actions">
              <a href= "/CustomerManage/OrderDetail?orderId=${order.orderID}" class="btn btn-sm btn-outline-primary view-details-btn">
                <i class="fas fa-eye me-1"></i> Chi tiết
              </a>
              <button class="btn btn-sm btn-outline-secondary print-order-btn" data-order-id="${order.orderID}">
                <i class="fas fa-print me-1"></i> In hóa đơn
              </button>
            </div>
          `;
          orderElement.appendChild(orderFooter);

          orderHistoryList.appendChild(orderElement);
      });

      document.querySelectorAll(".print-order-btn").forEach(btn => {
          btn.addEventListener("click", () => {
              const orderId = btn.getAttribute("data-order-id");
              alert(`Đơn hàng ${orderId} sẽ được in`);
          });
      });
  }
    function setupModalCleanup() {
        DOM.orderHistoryModal.addEventListener('hidden.bs.modal', function () {
            const backdrops = document.querySelectorAll('.modal-backdrop');
            backdrops.forEach(backdrop => backdrop.remove());

            document.body.classList.remove('modal-open');
            document.body.style.overflow = '';
            document.body.style.paddingRight = '';
        });

        // Xử lý sự kiện khi người dùng chuyển trang (sử dụng beforeunload)
        window.addEventListener('beforeunload', function () {
            // Đóng modal nếu đang mở
            const currentModal = bootstrap.Modal.getInstance(DOM.orderHistoryModal);
            if (currentModal) {
                currentModal.hide();
                currentModal.dispose();
            }

            // Xóa tất cả modal-backdrop
            const backdrops = document.querySelectorAll('.modal-backdrop');
            backdrops.forEach(backdrop => backdrop.remove());

            // Đảm bảo body không còn class modal-open
            document.body.classList.remove('modal-open');
            document.body.style.overflow = '';
            document.body.style.paddingRight = '';
        });
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

    function getOrderStatusText(status) {
        const statusMap = {
            0: "Chờ xử lý",
            1: "Đang xử lý",
            2: "Hoàn thành",
            3: "Đã hủy"
        };
        return statusMap[status] || "Chờ xử lý";
    }

    // Event Listeners
    function setupEventListeners() {
        let searchTimeout;
        DOM.userSearchInput?.addEventListener("input", function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => loadUsers(state.pagination.currentPage, this.value.toLowerCase(), state.statusFilter, state.sortFilter), 300);
        });

        DOM.statusFilter?.addEventListener("change", () => loadUsers(state.pagination.currentPage, state.searchTerm, DOM.statusFilter.value, state.sortFilter));
        DOM.sortFilter?.addEventListener("change", () => loadUsers(state.pagination.currentPage, state.searchTerm, state.statusFilter, DOM.sortFilter.value));

        DOM.saveUserBtn?.addEventListener("click", createUser);
        DOM.saveEditUserBtn?.addEventListener("click", updateUser);

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
            const editBtn = e.target.closest(".edit-customer-btn");
            const deleteBtn = e.target.closest(".delete-customer-btn");
            const historyBtn = e.target.closest(".view-history-btn");

            if (editBtn) openEditUserModal(parseInt(editBtn.getAttribute("data-customer-id")));
            if (deleteBtn) openDeleteUserModal(parseInt(deleteBtn.getAttribute("data-customer-id")));
            if (historyBtn) {
                const customerId = historyBtn.getAttribute("data-customer-id");
                const customerCard = historyBtn.closest(".customer-card");
                const customerName = customerCard.querySelector(".customer-name").textContent.trim();
                const customerPhone = customerCard.querySelector(".contact-item:first-child span").textContent.replace("SĐT:", "").trim();
                loadUserOrders(customerId, customerName, customerPhone);
            }
        });
    }

    // CRUD Operations
    async function createUser() {
        const userData = {
            fullName: document.getElementById("customerFullName").value.trim(),
            email: document.getElementById("customerEmail").value.trim(),
            phoneNumber: document.getElementById("customerPhone").value.trim(),
            passwordHash: document.getElementById("customerPassword").value.trim(),
            address: document.getElementById("customerAddress").value.trim(),
            userType: 0,
            status: parseInt(document.getElementById("customerStatus").value)
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
                document.getElementById("addCustomerForm").reset();
                bootstrap.Modal.getInstance(document.getElementById('addCustomerModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi thêm người dùng", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function updateUser() {
        const userData = {
            userID: parseInt(document.getElementById("editCustomerId").value),
            fullName: document.getElementById("editCustomerName").value.trim(),
            email: document.getElementById("editCustomerEmail").value.trim(),
            address: document.getElementById("editCustomerAddress").value.trim(),
            phoneNumber: document.getElementById("editCustomerPhone").value.trim(),
            status: parseInt(document.getElementById("editCustomerStatus").value)
        };

        const errors = validateUser(userData);
        if (errors.length > 0) {
            utils.showToast(errors.join("<br>"), "error");
            return;
        }

        const password = document.getElementById("editCustomerPassword").value.trim();
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
                bootstrap.Modal.getInstance(document.getElementById('editCustomerModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi cập nhật người dùng", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function openDeleteUserModal(userId) {
        const user = state.users.find(u => u.userID === userId);
        if (!user) return;

        console.log("User to delete:", user.fullName);
        document.getElementById("deleteFullName").innerHTML = user.fullName;
        document.getElementById("deleteCustomerId").value = user.userID;
        const deleteModal = new bootstrap.Modal(document.getElementById("deleteCustomerModal"));
        deleteModal.show();

        const confirmBtn = document.getElementById("confirmDeleteCustomerBtn");
        const deleteHandler = async () => {
            try {
                utils.showLoadingOverlay(true);
                const result = await utils.fetchData(API.deleteUser(userId), 'POST');
                if (result.success) {
                    utils.showToast("Xóa người dùng thành công", "success");
                    await loadUsers(state.pagination.currentPage, state.searchTerm, state.statusFilter);
                    deleteModal.hide();
                } else {
                    utils.showToast(result.message || "Lỗi khi xóa người dùng", "error");
                }
            } catch (error) {
                utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
            } finally {
                utils.showLoadingOverlay(false);
                confirmBtn.removeEventListener("click", deleteHandler);
            }
        };
        confirmBtn.addEventListener("click", deleteHandler);
    }

    async function openEditUserModal(userId) {
        const user = state.users.find(u => u.userID === userId);
        if (!user) return;

        document.getElementById("editCustomerId").value = user.userID;
        document.getElementById("editCustomerName").value = user.fullName;
        document.getElementById("editCustomerEmail").value = user.email || "";
        document.getElementById("editCustomerPhone").value = user.phoneNumber || "";
        document.getElementById("editCustomerStatus").value = user.status || 0;
        document.getElementById("editCustomerPassword").value = "";
        document.getElementById("editCustomerAddress").value = user.address || "";

        new bootstrap.Modal(document.getElementById("editCustomerModal")).show();
    }

    // Validation
    function validateUser(userData) {
        const errors = [];
        

        if (!userData.fullName || userData.fullName.trim() === '') errors.push("Họ tên không được để trống");
        else if (userData.fullName.length > 100) errors.push("Họ tên không được vượt quá 100 ký tự");

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

        return errors;
    }

    // Initialization
    async function initialize() {
        try {
            await Promise.all([
                loadUsers(1)
            ]);
            setupEventListeners();

            setupModalCleanup();
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi tải dữ liệu", "error");
        }
    }

    initialize();
});