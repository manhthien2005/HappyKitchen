document.addEventListener("DOMContentLoaded", function () {
    document.querySelector(".form-box").addEventListener("submit", async function (event) {
        event.preventDefault();

        const email = document.getElementById("email").value.trim();
        const password = document.getElementById("password").value.trim();
        const loginBtn = document.getElementById("login-btn");
        const loginText = document.getElementById("login-text");
        const loadingIcon = document.getElementById("loading-icon");

        if (!email || !password) {
            toastr.warning("Vui lòng nhập đầy đủ thông tin.");
            return;
        }

        // Hiển thị loading
        loginBtn.disabled = true;
        loginText.textContent = "Đang xử lý...";
        loadingIcon.classList.remove("d-none");

        try {
            const response = await fetch("/Home/Login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ Email: email, Password: password })
            });

            const data = await response.json();

            if (data.success) {
                toastr.success(data.message);

            } else {
                toastr.error(data.message);
            }
        } catch (error) {
            toastr.error("Đã xảy ra lỗi. Vui lòng thử lại!");
        } finally {
            // Ẩn loading
            loginBtn.disabled = false;
            loginText.textContent = "Đăng Nhập";
            loadingIcon.classList.add("d-none");
        }
    });
});
