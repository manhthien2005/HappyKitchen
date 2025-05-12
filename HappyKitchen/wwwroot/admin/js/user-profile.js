document.addEventListener("DOMContentLoaded", () => {
    // DOM Elements
    const DOM = {
        userProfileForm: document.getElementById("userProfileForm"),
        userId: document.getElementById("userId"),
        fullName: document.getElementById("fullName"),
        email: document.getElementById("email"),
        address: document.getElementById("address"),
        password: document.getElementById("password"),
        confirmPassword: document.getElementById("confirmPassword"),
        saveProfileBtn: document.getElementById("saveProfileBtn")
    };

    // API Endpoints
    const API = {
        getProfile: '/Admin/GetUserProfile',
        updateProfile: '/Admin/UpdateUserProfile'
    };

    // Utility Functions
    const utils = window.utils || {
        fetchData: async (url, method = 'GET', body = null) => {
            try {
                const options = {
                    method,
                    headers: { 'Content-Type': 'application/json' }
                };
                if (body) options.body = JSON.stringify(body);
                const response = await fetch(url, options);
                if (!response.ok) {
                    throw new Error(`HTTP error! Status: ${response.status}`);
                }
                return await response.json();
            } catch (error) {
                console.error('Fetch error:', error);
                throw error;
            }
        },
        showToast: (message, type) => {
            alert(`${type.toUpperCase()}: ${message}`); // Thay bằng toastr nếu có
        },
        showLoadingOverlay: (show) => {
            console.log(show ? "Loading..." : "Loading complete");
        }
    };

    // Load User Profile
    async function loadUserProfile() {
        try {
            const result = await utils.fetchData(API.getProfile);
            if (result.success) {
                DOM.userId.value = result.data.userID;
                DOM.fullName.value = result.data.fullName;
                DOM.email.value = result.data.email || "";
                DOM.address.value = result.data.address || "";
            } else {
                utils.showToast(result.message || "Không thể tải thông tin cá nhân", "error");
            }
        } catch (error) {
            utils.showToast("Lỗi khi tải thông tin cá nhân: " + error.message, "error");
            console.error('Load profile error:', error);
        }
    }

    // Save User Profile
    async function saveProfile() {
        const profileData = {
            UserID: parseInt(DOM.userId.value) || 0,
            FullName: DOM.fullName.value.trim(),
            Email: DOM.email.value.trim() || null,
            Address: DOM.address.value.trim() || null,
            Password: DOM.password.value.trim() || null,
            ConfirmPassword: DOM.confirmPassword.value.trim() || null
        };

        // Client-side validation
        if (!profileData.UserID) {
            utils.showToast("ID người dùng không hợp lệ", "error");
            return;
        }
        if (!profileData.FullName) {
            utils.showToast("Họ và tên là bắt buộc", "error");
            return;
        }
        if (!profileData.Email) {
            utils.showToast("Email là bắt buộc", "error");
            return;
        }
        if (profileData.Password && profileData.Password !== profileData.ConfirmPassword) {
            utils.showToast("Mật khẩu xác nhận không khớp", "error");
            return;
        }

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.updateProfile, 'POST', profileData);
            if (result.success) {
                utils.showToast(result.message || "Cập nhật thông tin thành công", "success");
                // Tải lại thông tin để đảm bảo dữ liệu đồng bộ
                await loadUserProfile();
            } else {
                utils.showToast(result.message || "Lỗi khi cập nhật thông tin", "error");
            }
        } catch (error) {
            utils.showToast("Lỗi khi cập nhật thông tin: " + error.message, "error");
            console.error('Save profile error:', error);
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Event Listeners
    function setupEventListeners() {
        DOM.saveProfileBtn.addEventListener("click", saveProfile);
    }

    // Initialize
    async function initialize() {
        try {
            await loadUserProfile();
            setupEventListeners();
        } catch (error) {
            utils.showToast("Đã xảy ra lỗi khi khởi tạo trang: " + error.message, "error");
            console.error("Initialization error:", error);
        }
    }

    initialize();
});