document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        products: [],
        categories: [],
        pagination: {
            currentPage: 1,
            pageSize: 9,
            totalItems: 0,
            totalPages: 0
        },
        searchTerm: "",
        statusFilter: "all",
        categoryFilter: 0,
        sortFilter: "name_asc",
        isImageDeleted: false
    };

    // API Endpoints
    const API = {
        products: (page, pageSize, searchTerm, statusFilter, categoryId, sortFilter) =>
            `/ProductManage/GetMenuItems?page=${page}&pageSize=${pageSize}` +
            (searchTerm ? `&searchTerm=${encodeURIComponent(searchTerm)}` : '') +
            (statusFilter !== 'all' ? `&status=${statusFilter}` : '') +
            (categoryId > 0 ? `&categoryId=${categoryId}` : '') +
            (sortFilter !== 'name_asc' ? `&sortBy=${sortFilter}` : ''),

        categories: '/ProductManage/GetCategories',
        createProduct: '/ProductManage/CreateMenuItem',
        updateProduct: '/ProductManage/UpdateMenuItem',
        deleteProduct: (id) => `/ProductManage/DeleteMenuItem?id=${id}`,
        createCategory: '/ProductManage/CreateCategory'
    };

    // DOM Elements
    const DOM = {
        productsGrid: document.getElementById("productsGrid"),
        categoriesList: document.getElementById("categoriesList"),
        productSearchInput: document.getElementById("productSearchInput"),
        saveProductBtn: document.getElementById("saveProductBtn"),
        updateProductBtn: document.getElementById("updateProductBtn"),
        saveCategoryBtn: document.getElementById("saveCategoryBtn"),
        paginationContainer: document.getElementById("productPagination"),
        paginationPrev: document.getElementById("paginationPrev"),
        paginationNext: document.getElementById("paginationNext"),
        paginationItems: document.getElementById("paginationItems"),
        statusFilter: document.getElementById("statusFilter"),
        sortFilter: document.getElementById("sortFilter"),
        productCategory: document.getElementById("productCategory"),
        editProductCategory: document.getElementById("editProductCategory"),
        imagePreview: document.getElementById("imagePreview"),
        editImagePreview: document.getElementById("editImagePreview"),
        productImage: document.getElementById("productImage"),
        editProductImage: document.getElementById("editProductImage"),
        removeImageBtn: document.getElementById("removeImageBtn"),
        editRemoveImageBtn: document.getElementById("editRemoveImageBtn"),
        productAttributes: document.getElementById("productAttributes"),
        editProductAttributes: document.getElementById("editProductAttributes")
    };

    // Skeleton Loaders
    function showProductSkeletons() {
        const skeletonCount = 8;
        DOM.productsGrid.innerHTML = Array(skeletonCount).fill().map(() => `
            <div class="col">
                <div class="product-card">
                    <div class="product-card-image skeleton" style="height: 150px;"></div>
                    <div class="product-card-body">
                        <h5 class="skeleton" style="width: 80%; height: 24px;"></h5>
                        <div class="skeleton" style="width: 60%; height: 16px;"></div>
                        <div class="skeleton" style="width: 40%; height: 16px;"></div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    // Data Loading
    async function loadProducts(page = 1, searchTerm = state.searchTerm, statusFilter = state.statusFilter, categoryId = state.categoryFilter, sortFilter = state.sortFilter) {
        try {
            showProductSkeletons();
            
            state.pagination.currentPage = page;
            state.searchTerm = searchTerm;
            state.statusFilter = statusFilter;
            state.categoryFilter = categoryId;
            state.sortFilter = sortFilter;

            const url = API.products(page, state.pagination.pageSize, searchTerm, statusFilter, categoryId, sortFilter);
            const result = await utils.fetchData(url);

            if (result.success) {
                state.products = result.data;
                state.pagination = result.pagination;
                renderProducts();
                renderPagination();
            } else {
                utils.showToast(result.message || "Không thể tải danh sách sản phẩm", "error");
            }
        } catch (error) {
            console.error('Load products error:', error);
            utils.showToast("Không thể tải danh sách sản phẩm", "error");
        }
    }

    async function loadCategories() {
        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.categories);

            if (result.success) {
                state.categories = result.data;
                renderCategories();
                renderCategoryOptions();
            } else {
                utils.showToast(result.message || "Không thể tải danh mục", "error");
            }
        } catch (error) {
            console.error('Load categories error:', error);
            utils.showToast("Không thể tải danh mục", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Rendering Functions
    function renderProducts() {
        DOM.productsGrid.innerHTML = state.products.length === 0
            ? `
                <div class="col-12 d-flex align-items-center justify-content-center w-100" style="min-height: 200px">
                    <div class="text-center">
                        <i class="fas fa-box-open fa-3x text-muted mb-3"></i>
                        <p class="text-muted mb-0">Không tìm thấy sản phẩm</p>
                    </div>
                </div>
            `
            : state.products.map(product => `
                <div class="col">
                    <div class="product-card">
                        <div class="product-card-image">
                            <div class="product-status-badge ${product.status === 1 ? 'status-badge-active' : 'status-badge-inactive'}">
                                ${product.status === 1 ? 'Còn hàng' : 'Hết hàng'}
                            </div>
                            ${product.menuItemImage ? 
                                `<img src="/images/MenuItem/${product.menuItemImage}" alt="${product.name}">` :
                                `<div class="no-image-placeholder"><i class="fas fa-image"></i><span>Không có ảnh</span></div>`}
                        </div>
                        <div class="product-card-body">
                            <div style="display: flex; justify-content: space-between;">
                                <h5 class="product-card-title">${product.name}</h5>
                                <span class="review-rating">
                                    ${product.averageRating.toFixed(2)}<i class="fa-solid fa-star"></i>
                                </span>
                            </div>

                            <div class="product-card-category">${product.categoryName || 'Chưa có danh mục'}</div>
                            <div class="product-card-price">${utils.formatMoney(product.price)}</div>
                            <div class="product-card-attributes">
                                ${product.attributes?.map(attr => `
                                    <div class="product-attribute">
                                        <span class="attribute-name">${attr.attributeName}:</span>
                                        <span class="attribute-value">${attr.attributeValue}</span>
                                    </div>
                                `).join('') || ''}
                            </div>
                        </div>
                        <div class="product-card-footer">
                            <button class="btn btn-sm btn-outline-primary edit-product-btn" data-product-id="${product.menuItemID}" data-bs-toggle="modal" data-bs-target="#editProductModal">
                                <i class="fas fa-edit"></i> Sửa
                            </button>
                            <button class="btn btn-sm btn-outline-danger delete-product-btn" data-product-id="${product.menuItemID}">
                                <i class="fas fa-trash"></i> Xóa
                            </button>
                        </div>
                    </div>
                </div>
            `).join('');
    }

    function renderCategories() {
        const totalProducts = state.categories.reduce((sum, category) => sum + category.productCount, 0);
        DOM.categoriesList.innerHTML = `
            <li class="category-item ${state.categoryFilter === 0 ? 'active' : ''}" data-category="0">
                <span class="category-name">Tất cả Sản phẩm</span>
                <span class="category-count">${totalProducts}</span>
            </li>
            ${state.categories.map(category => `
                <li class="category-item ${state.categoryFilter === category.categoryID ? 'active' : ''}" data-category="${category.categoryID}">
                    <span class="category-name">${category.categoryName}</span>
                    <span class="category-count">${category.productCount}</span>
                </li>
            `).join('')}
        `;
    }

    function renderCategoryOptions() {
        const options = state.categories.map(category => `
            <option value="${category.categoryID}">${category.categoryName}</option>
        `).join('');
        DOM.productCategory.innerHTML = `<option value="" selected disabled>Chọn danh mục</option>${options}`;
        DOM.editProductCategory.innerHTML = `<option value="" selected disabled>Chọn danh mục</option>${options}`;
    }

    function renderPagination() {
        utils.renderPagination(
            state.pagination,
            DOM.paginationContainer,
            DOM.paginationItems,
            DOM.paginationPrev,
            DOM.paginationNext,
            (pageNum) => loadProducts(pageNum, state.searchTerm, state.statusFilter, state.categoryFilter, state.sortFilter)
        );
    }

    // Form Reset Functions
    function resetAddForm() {
        const form = document.getElementById("addProductForm");
        form.reset();
        DOM.imagePreview.innerHTML = `
            <div class="placeholder-text">
                <i class="fas fa-image"></i>
                <p>Tải lên hình ảnh sản phẩm</p>
            </div>`;
        DOM.productAttributes.innerHTML = "";
        DOM.removeImageBtn.disabled = true;
        console.log("Add form reset");
    }

    function resetEditForm() {
        const form = document.getElementById("editProductForm");
        form.reset();
        DOM.editImagePreview.innerHTML = `
            <div class="placeholder-text">
                <i class="fas fa-image"></i>
                <p>Tải lên hình ảnh sản phẩm</p>
            </div>`;
        DOM.editProductAttributes.innerHTML = "";
        DOM.editRemoveImageBtn.disabled = true;
        console.log("Edit form reset");
    }

    // Event Listeners
    function setupEventListeners() {
        let searchTimeout;
        DOM.productSearchInput?.addEventListener("input", function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => loadProducts(state.pagination.currentPage, this.value.toLowerCase(), state.statusFilter, state.categoryFilter, state.sortFilter), 300);
        });

        DOM.saveProductBtn?.addEventListener("click", createProduct);
        DOM.updateProductBtn?.addEventListener("click", updateProduct);
        DOM.saveCategoryBtn?.addEventListener("click", createCategory);

        DOM.paginationPrev?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage > 1) {
                loadProducts(state.pagination.currentPage - 1, state.searchTerm, state.statusFilter, state.categoryFilter, state.sortFilter);
            }
        });

        DOM.paginationNext?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage < state.pagination.totalPages) {
                loadProducts(state.pagination.currentPage + 1, state.searchTerm, state.statusFilter, state.categoryFilter, state.sortFilter);
            }
        });

        DOM.statusFilter?.addEventListener("click", (e) => {
            const btn = e.target.closest("button[data-status]");
            if (btn) {
                DOM.statusFilter.querySelectorAll("button").forEach(b => b.classList.remove("active"));
                btn.classList.add("active");
                const status = btn.getAttribute("data-status");
                loadProducts(state.pagination.currentPage, state.searchTerm, status, state.categoryFilter, state.sortFilter);
            }
        });

        DOM.sortFilter?.addEventListener("change", function () {
            loadProducts(state.pagination.currentPage, state.searchTerm, state.statusFilter, state.categoryFilter, this.value);
        });

        document.addEventListener("click", (e) => {
            const editBtn = e.target.closest(".edit-product-btn");
            const deleteBtn = e.target.closest(".delete-product-btn");
            const categoryItem = e.target.closest(".category-item");

            if (editBtn) openEditProductModal(parseInt(editBtn.getAttribute("data-product-id")));
            if (deleteBtn) deleteProduct(parseInt(deleteBtn.getAttribute("data-product-id")));
            if (categoryItem) {
                document.querySelectorAll(".category-item").forEach(item => item.classList.remove("active"));
                categoryItem.classList.add("active");
                const categoryId = parseInt(categoryItem.getAttribute("data-category"));
                loadProducts(state.pagination.currentPage, state.searchTerm, state.statusFilter, categoryId, state.sortFilter);
            }
        });

        ['uploadImageBtn', 'editUploadImageBtn'].forEach(id => {
            const btn = document.getElementById(id);
            const input = document.getElementById(id === 'uploadImageBtn' ? 'productImage' : 'editProductImage');
            const preview = document.getElementById(id === 'uploadImageBtn' ? 'imagePreview' : 'editImagePreview');
            const removeBtn = document.getElementById(id === 'uploadImageBtn' ? 'removeImageBtn' : 'editRemoveImageBtn');
        
            if (btn && input) {
                btn.addEventListener("click", () => input.click());
                input.addEventListener("change", (e) => {
                    const file = e.target.files[0];
                    if (file) {
                        const reader = new FileReader();
                        reader.onload = (e) => {
                            preview.innerHTML = `<img src="${e.target.result}" alt="Product Image">`;
                            removeBtn.disabled = false;
                            state.isImageDeleted = false; 
                            console.log(`Image selected for ${id}`, file.name);
                        };
                        reader.readAsDataURL(file);
                    }
                });
            }
        
            if (removeBtn) {
                removeBtn.addEventListener("click", () => {
                    input.value = "";
                    preview.innerHTML = `
                        <div class="placeholder-text">
                            <i class="fas fa-image"></i>
                            <p>Tải lên hình ảnh sản phẩm</p>
                        </div>`;
                    removeBtn.disabled = true;
                    state.isImageDeleted = true; 
                    console.log(`Image removed for ${id}`);
                });
            }
        });
        
        // Add attribute
        ['addAttributeBtn', 'editAddAttributeBtn'].forEach(id => {
            const btn = document.getElementById(id);
            if (btn) {
                btn.addEventListener("click", () => {
                    const container = document.getElementById(id === 'addAttributeBtn' ? 'productAttributes' : 'editProductAttributes');
                    // id attribute
                    const attributeId = Date.now();
                    const attributeItem = document.createElement("div");
                        attributeItem.className = "attribute-item";
                        attributeItem.innerHTML = `
                            <div class="row g-2">
                                <div class="col-5">
                                    <input type="text" class="form-control form-control-sm attribute-name" placeholder="Tên thuộc tính" required>
                                </div>
                                <div class="col-6">
                                    <input type="text" class="form-control form-control-sm attribute-value" placeholder="Giá trị" required>
                                </div>
                                <div class="col-1 d-flex align-items-center">
                                    <i class="fas fa-times attribute-remove" data-attribute-id="${attributeId}"></i>
                                </div>
                            </div>
                        `;
                    container.appendChild(attributeItem);

                    attributeItem.querySelector(".attribute-remove").addEventListener("click", () => {
                        attributeItem.remove();
                        console.log(`Attribute removed for ${id}`, { attributeId });
                    });

                    console.log(`Attribute added for ${id}`, { attributeId });
                });
            }
        });

        // Modal close cleanup
        document.getElementById("addProductModal")?.addEventListener("hidden.bs.modal", resetAddForm);
        document.getElementById("editProductModal")?.addEventListener("hidden.bs.modal", resetEditForm);
    }
    // CRUD Operations
    async function createProduct() {
        console.log("Starting createProduct function");
        const form = document.getElementById("addProductForm");
        if (!form.checkValidity()) {
            console.log("Form validation failed");
            form.reportValidity();
            return;
        }
        console.log("Form validation passed");

        const formData = new FormData();
        const menuItem = {
            Name: document.getElementById("productName").value,
            Price: parseFloat(document.getElementById("productPrice").value),
            CategoryID: parseInt(document.getElementById("productCategory").value),
            Description: document.getElementById("productDescription").value,
            Status: document.getElementById("productStatus").value === "active" ? 1 : 0
        };
        console.log("Product data collected:", menuItem);

        const attributes = [];
        let attributeError = false;
        const attributeItems = document.querySelectorAll("#productAttributes .attribute-item");
        console.log(`Found ${attributeItems.length} attribute items to process`);
        
        attributeItems.forEach((item, index) => {
            const name = item.querySelector(".attribute-name").value.trim();
            const value = item.querySelector(".attribute-value").value.trim();
            console.log(`Processing attribute #${index + 1}:`, { name, value });
            
            if (name && value) {
                attributes.push({ AttributeName: name, AttributeValue: value });
                console.log(`Attribute #${index + 1} added successfully`);
            } else if (name || value) {
                attributeError = true;
                console.log(`Attribute #${index + 1} validation failed - missing ${!name ? 'name' : 'value'}`);
                item.querySelector(".attribute-name").reportValidity();
                item.querySelector(".attribute-value").reportValidity();
            } else {
                console.log(`Attribute #${index + 1} skipped - both name and value empty`);
            }
        });

        if (attributeError) {
            console.log("Attribute validation failed - stopping form submission");
            utils.showToast("Vui lòng điền đầy đủ tên và giá trị cho tất cả thuộc tính", "error");
            console.log("Attribute validation failed", attributes);
            return;
        }
        console.log(`All ${attributes.length} attributes validated successfully`);

        // Fix: Use proper form field names that match the model structure
        formData.append("MenuItem.Name", menuItem.Name);
        formData.append("MenuItem.Price", menuItem.Price);
        formData.append("MenuItem.CategoryID", menuItem.CategoryID);
        formData.append("MenuItem.Description", menuItem.Description);
        formData.append("MenuItem.Status", menuItem.Status);
        
        // Add attributes as individual form fields
        attributes.forEach((attr, index) => {
            formData.append(`Attributes[${index}].AttributeName`, attr.AttributeName);
            formData.append(`Attributes[${index}].AttributeValue`, attr.AttributeValue);
        });
        
        console.log("Creating product", { menuItem, attributes });

        const image = DOM.productImage.files[0];
        if (image) {
            console.log("Image found for upload:", { 
                name: image.name, 
                size: image.size, 
                type: image.type,
                lastModified: new Date(image.lastModified).toISOString()
            });
            formData.append("image", image);
        } else {
            console.log("No image selected for upload");
        }
        
        try {
            console.log("Showing loading overlay and starting API request");
            utils.showLoadingOverlay(true);
            console.log("Sending POST request to:", API.createProduct);
            
            const response = await fetch(API.createProduct, {
                method: 'POST',
                body: formData
            });
            console.log("API response received:", { 
                status: response.status, 
                statusText: response.statusText,
                headers: Object.fromEntries([...response.headers])
            });
            
            const result = await response.json();
            console.log("Create product result:", result);

            if (result.success) {
                console.log("Product created successfully");
                utils.showToast("Thêm sản phẩm thành công", "success");
                console.log("Reloading products and categories");
                await Promise.all([loadProducts(state.pagination.currentPage, state.searchTerm, status, state.categoryFilter, state.sortFilter), loadCategories()]);
                console.log("Resetting form");
                resetAddForm();
                console.log("Closing modal");
                bootstrap.Modal.getInstance(document.getElementById('addProductModal')).hide();
            } else {
                console.error("API returned error:", result.message);
                utils.showToast(result.message || "Lỗi khi thêm sản phẩm", "error");
            }
        } catch (error) {
            console.error("Create product error:", error);
            console.error("Error details:", { 
                name: error.name, 
                message: error.message, 
                stack: error.stack 
            });
            utils.showToast("Đã xảy ra lỗi khi thêm sản phẩm", "error");
        } finally {
            console.log("Hiding loading overlay");
            utils.showLoadingOverlay(false);
            console.log("createProduct function completed");
        }
    }

    async function updateProduct() {
        console.log("Starting updateProduct function");
        const form = document.getElementById("editProductForm");
        if (!form.checkValidity()) {
            console.log("Edit form validation failed");
            form.reportValidity();
            return;
        }
        console.log("Edit form validation passed");

        const formData = new FormData();
        const menuItem = {
            MenuItemID: parseInt(document.getElementById("editProductId").value),
            Name: document.getElementById("editProductName").value,
            Price: parseFloat(document.getElementById("editProductPrice").value),
            CategoryID: parseInt(document.getElementById("editProductCategory").value),
            Description: document.getElementById("editProductDescription").value,
            Status: document.getElementById("editProductStatus").value === "active" ? 1 : 0
        };
        console.log("Product update data collected:", menuItem);

        const attributes = [];
        let attributeError = false;
        const attributeItems = document.querySelectorAll("#editProductAttributes .attribute-item");
        console.log(`Found ${attributeItems.length} attribute items to process for update`);
        
        attributeItems.forEach((item, index) => {
            const name = item.querySelector(".attribute-name").value.trim();
            const value = item.querySelector(".attribute-value").value.trim();
            console.log(`Processing update attribute #${index + 1}:`, { name, value });
            
            if (name && value) {
                attributes.push({ AttributeName: name, AttributeValue: value });
                console.log(`Update attribute #${index + 1} added successfully`);
            } else if (name || value) {
                attributeError = true;
                console.log(`Update attribute #${index + 1} validation failed - missing ${!name ? 'name' : 'value'}`);
                item.querySelector(".attribute-name").reportValidity();
                item.querySelector(".attribute-value").reportValidity();
            } else {
                console.log(`Update attribute #${index + 1} skipped - both name and value empty`);
            }
        });

        if (attributeError) {
            console.log("Update attribute validation failed - stopping form submission");
            utils.showToast("Vui lòng điền đầy đủ tên và giá trị cho tất cả thuộc tính", "error");
            console.log("Attribute validation failed", attributes);
            return;
        }
        console.log(`All ${attributes.length} update attributes validated successfully`);

        // Fix: Use proper form field names that match the model structure
        formData.append("MenuItem.MenuItemID", menuItem.MenuItemID);
        formData.append("MenuItem.Name", menuItem.Name);
        formData.append("MenuItem.Price", menuItem.Price);
        formData.append("MenuItem.CategoryID", menuItem.CategoryID);
        formData.append("MenuItem.Description", menuItem.Description);
        formData.append("MenuItem.Status", menuItem.Status);
        
        // Add attributes as individual form fields
        attributes.forEach((attr, index) => {
            formData.append(`Attributes[${index}].AttributeName`, attr.AttributeName);
            formData.append(`Attributes[${index}].AttributeValue`, attr.AttributeValue);
        });
        
        console.log("Updating product", { menuItem, attributes });

        const image = document.getElementById("editProductImage").files[0];
        if (image) {
            console.log("Update image found for upload:", { 
                name: image.name, 
                size: image.size, 
                type: image.type,
                lastModified: new Date(image.lastModified).toISOString()
            });
            formData.append("image", image);
            state.isImageDeleted = false; 
        } else if (state.isImageDeleted) {
            console.log("Image deletion requested");
            formData.append("isDeleted", "true"); 
        } else {
            console.log("No new image selected and no deletion requested");
        }

        console.log("Updating product", { menuItem, attributes, hasImage: !!image, state:state.isImageDeleted  });
        
        try {
            console.log("Showing loading overlay and starting update API request");
            utils.showLoadingOverlay(true);
            console.log("Sending POST request to:", API.updateProduct);
            
            const response = await fetch(API.updateProduct, {
                method: 'POST',
                body: formData
            });
            console.log("Update API response received:", { 
                status: response.status, 
                statusText: response.statusText,
                headers: Object.fromEntries([...response.headers])
            });
            
            const result = await response.json();
            console.log("Update product result:", result);

            if (result.success) {
                console.log("Product updated successfully");
                utils.showToast("Cập nhật sản phẩm thành công", "success");
                console.log("Reloading products and categories after update");
                await Promise.all([loadProducts(state.pagination.currentPage, state.searchTerm, status, state.categoryFilter, state.sortFilter), loadCategories()]);
                console.log("Resetting edit form");
                resetEditForm();
                console.log("Closing edit modal");
                bootstrap.Modal.getInstance(document.getElementById('editProductModal')).hide();
            } else {
                console.error("Update API returned error:", result.message);
                utils.showToast(result.message || "Lỗi khi cập nhật sản phẩm", "error");
            }
        } catch (error) {
            console.error("Update product error:", error);
            console.error("Update error details:", { 
                name: error.name, 
                message: error.message, 
                stack: error.stack 
            });
            utils.showToast("Đã xảy ra lỗi khi cập nhật sản phẩm", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function deleteProduct(productId) {
        const product = state.products.find(u => u.menuItemID === productId);
        if (!product) return;
        
        console.log("User to delete:", product.menuItemID);
        document.getElementById("deleteProductName").innerHTML = product.name;
        document.getElementById("deleteProductId").value = product.menuItemID;
        const deleteModal = new bootstrap.Modal(document.getElementById("deleteProductModal"));
        deleteModal.show();

        const confirmBtn = document.getElementById("confirmDeleteProductBtn");
        const deleteHandler = async () => {
            try {
                utils.showLoadingOverlay(true);
                console.log("Deleting product", { productId });
                const result = await utils.fetchData(API.deleteProduct(productId), 'POST');
                console.log("Delete product result", result);

                if (result.success) {
                    utils.showToast("Xóa sản phẩm thành công", "success");
                    await Promise.all([loadProducts(state.pagination.currentPage, state.searchTerm, status, state.categoryFilter, state.sortFilter), loadCategories()]);
                    deleteModal.hide();
                } else {
                    utils.showToast(result.message || "Lỗi khi xóa sản phẩm", "error");
                }
            } catch (error) {
                console.error("Delete product error", error);
                utils.showToast("Đã xảy ra lỗi khi xóa sản phẩm", "error");
            } finally {
                utils.showLoadingOverlay(false);
                confirmBtn.removeEventListener("click", deleteHandler);
            }
        };

        confirmBtn.addEventListener("click", deleteHandler);
    }

    async function createCategory() {
        const form = document.getElementById("addCategoryForm");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const categoryName = document.getElementById("categoryName").value;
        console.log("Creating category", { categoryName });

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.createCategory, 'POST', { categoryName });
            console.log("Create category result", result);

            if (result.success) {
                utils.showToast("Thêm danh mục thành công", "success");
                await loadCategories();
                form.reset();
                bootstrap.Modal.getInstance(document.getElementById('addCategoryModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi thêm danh mục", "error");
            }
        } catch (error) {
            console.error("Create category error", error);
            utils.showToast("Đã xảy ra lỗi khi thêm danh mục", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function openEditProductModal(productId) {
        const product = state.products.find(p => p.menuItemID === productId);
        if (!product) {
            console.error("Product not found for edit", { productId });
            return;
        }

        console.log("Opening edit modal", { product });
        document.getElementById("editProductId").value = product.menuItemID;
        document.getElementById("editProductName").value = product.name;
        document.getElementById("editProductCategory").value = product.categoryID;
        document.getElementById("editProductPrice").value = product.price;
        document.getElementById("editProductDescription").value = product.description || "";
        document.getElementById("editProductStatus").value = product.status === 1 ? "active" : "inactive";
        
        const attributesContainer = document.getElementById("editProductAttributes");
        attributesContainer.innerHTML = "";
        if (product.attributes && product.attributes.length > 0) {
            product.attributes.forEach(attr => {
                const attributeItem = document.createElement("div");
                attributeItem.className = "attribute-item";
                attributeItem.innerHTML = `
                    <div class="row g-2">
                        <div class="col-5">
                            <input type="text" class="form-control form-control-sm attribute-name" value="${attr.attributeName}" placeholder="Tên thuộc tính" required>
                        </div>
                        <div class="col-6">
                            <input type="text" class="form-control form-control-sm attribute-value" value="${attr.attributeValue}" placeholder="Giá trị" required>
                        </div>
                        <div class="col-1 d-flex align-items-center">
                            <i class="fas fa-times attribute-remove"></i>
                        </div>
                    </div>
                `;
                attributesContainer.appendChild(attributeItem);
                attributeItem.querySelector(".attribute-remove").addEventListener("click", () => {
                    attributeItem.remove();
                    console.log("Attribute removed in edit modal", { attributeName: attr.attributeName });
                });
            });
        }

        DOM.editImagePreview.innerHTML = product.menuItemImage ?
            `<img src="/images/MenuItem/${product.menuItemImage}" alt="Product Image">` :
            `<div class="placeholder-text"><i class="fas fa-image"></i><p>Tải lên hình ảnh sản phẩm</p></div>`;
        DOM.editRemoveImageBtn.disabled = !product.menuItemImage;
        DOM.editProductImage.value = ""; // Clear file input
    }

    // Initialize
    async function initialize() {
        try {
            await Promise.all([
                loadProducts(),
                loadCategories()
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