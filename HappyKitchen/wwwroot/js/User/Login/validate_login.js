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

        // Lấy reCAPTCHA token (ví dụ từ widget v2)
        const token = document.getElementById("g-recaptcha-response").value;
        if (!token) {
            toastr.error("Vui lòng xác thực reCAPTCHA.");
            loginBtn.disabled = false;
            loginText.textContent = "Đăng Nhập";
            loadingIcon.classList.add("d-none");
            return;
        }

        try {
            const response = await fetch("/User/Login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    Email: email,
                    Password: password,
                    RecaptchaToken: token
                })
            });

            const data = await response.json();

            if (data.success) {
                if (data.requireOTP) {
                    // Nếu yêu cầu OTP, chuyển hướng sang trang xác thực OTP
                    toastr.info(data.message);
                    setTimeout(() => {
                        window.location.href = data.redirectUrl;
                    }, 1500);
                } else {
                    toastr.success(data.message);
                    setTimeout(() => {
                        window.location.href = "/User/TEMP"; // Trang Menu hoặc trang chủ
                    }, 1500);
                }
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
