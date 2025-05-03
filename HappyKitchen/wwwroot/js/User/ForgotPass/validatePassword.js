document.querySelector("form").addEventListener("submit", function (event) {
    event.preventDefault();

    const loginBtn = document.getElementById("login-btn");
    const loginText = document.getElementById("login-text");
    const loadingIcon = document.getElementById("loading-icon");

    let email = document.getElementById("userEmail").value.trim();
    let newPassword = document.getElementById("password").value.trim();
    let confirmPassword = document.getElementById("confirm-password").value.trim();

    if (newPassword.length < 6) {
        toastr.error("Mật khẩu phải có ít nhất 6 ký tự!");
        return;
    }

    if (newPassword !== confirmPassword) {
        toastr.error("Mật khẩu nhập lại không khớp!");
        return;
    }

    let data = {
        Email: email,
        NewPassword: newPassword,
        ConfirmPassword: confirmPassword
    };

    console.log("Dữ liệu gửi đi:", data);

    // Hiển thị hiệu ứng loading
    loginText.innerText = "Đang xử lí...";
    loadingIcon.classList.remove("d-none");
    loginBtn.disabled = true;

    fetch("/User/ResetPassword", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(data)
    })
        .then(response => response.json())
        .then(result => {
            console.log("Phản hồi từ server:", result);

            if (result.success) {
                toastr.success("Cập nhật mật khẩu thành công!");

                loginBtn.disabled = true;
                loginText.innerText = "Đã cập nhật";
                loadingIcon.classList.add("d-none");

                setTimeout(() => window.location.href = "/User/Login", 2000);
            } else {
                toastr.error(result.message || "Đã có lỗi xảy ra!");
                loginText.innerText = "Đổi Mật Khẩu";
                loadingIcon.classList.add("d-none");
                loginBtn.disabled = false;
                if (result.errors) {
                    result.errors.forEach(error => toastr.error(error));
                }
            }
        })
        .catch(error => {
            console.error("Lỗi:", error);
            toastr.error("Lỗi kết nối đến server!");
            loginText.innerText = "Đổi Mật Khẩu";
            loadingIcon.classList.add("d-none");
            loginBtn.disabled = false;
        })

});




function togglePassword(fieldId) {
    let field = document.getElementById(fieldId);
    let icon = field.nextElementSibling.querySelector("i");
    if (field.type === "password") {
        field.type = "text";
        icon.classList.remove("fa-eye");
        icon.classList.add("fa-eye-slash");
    } else {
        field.type = "password";
        icon.classList.remove("fa-eye-slash");
        icon.classList.add("fa-eye");
    }
}
