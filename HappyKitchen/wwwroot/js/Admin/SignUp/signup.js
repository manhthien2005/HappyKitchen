function togglePassword(inputId) {
    var passwordInput = document.getElementById(inputId);
    var icon = passwordInput.nextElementSibling.querySelector("i");

    if (passwordInput.type === "password") {
        passwordInput.type = "text";
        icon.classList.replace("fa-eye", "fa-eye-slash");
    } else {
        passwordInput.type = "password";
        icon.classList.replace("fa-eye-slash", "fa-eye");
    }
}
document.addEventListener("DOMContentLoaded", function () {
    const fullName = document.getElementById("fullName");
    const phone = document.getElementById("phoneNumber");
    const email = document.getElementById("email");
    const password = document.getElementById("password");
    const confirmPassword = document.getElementById("confirm-password");

    const nameError = document.getElementById("nameError");
    const phoneError = document.getElementById("phoneError");
    const emailError = document.getElementById("emailError");
    const passError = document.getElementById("passError");
    const confirmPassError = document.getElementById("confirm-passError");

    function validateInput(input, errorElement, condition, errorMessage) {
        if (condition) {
            errorElement.textContent = errorMessage;
            input.style.marginBottom = "0";
        } else {
            errorElement.textContent = "";
            input.style.marginBottom = "20px";
        }
    }

    function adjustMargin(input, errorElement) {
        if (errorElement.textContent.trim() !== "") {
            input.style.marginBottom = "0";
        } else {
            input.style.marginBottom = "20px";
        }
    }

    // Kiểm tra khi trang load
    adjustMargin(phone, phoneError);
    adjustMargin(email, emailError);

    // Kiểm tra khi người dùng nhập
    fullName.addEventListener("input", function () {
        validateInput(fullName, nameError, fullName.value.trim() === "" || /\d/.test(fullName.value), "Họ và tên không được để trống hoặc chứa số");
    });

    phone.addEventListener("input", function () {
        let phoneRegex = /^[0-9]{10}$/; // Số điện thoại hợp lệ (10 số, bắt đầu 03,05,07,08,09)

        validateInput(phone, phoneError, !phoneRegex.test(phone.value), "Số điện thoại không hợp lệ");
    });

    email.addEventListener("input", function () {
        validateInput(email, emailError, !email.value.includes("@"), "Email không hợp lệ");
    });

    password.addEventListener("input", function () {
        validateInput(password, passError, password.value.length < 6, "Mật khẩu phải có ít nhất 6 ký tự");
    });

    confirmPassword.addEventListener("input", function () {
        validateInput(confirmPassword, confirmPassError, password.value !== confirmPassword.value, "Mật khẩu nhập lại không khớp");
    });

    // Kiểm tra toàn bộ form trước khi submit
    document.querySelector("form").addEventListener("submit", function (event) {
        let isValid = true;

        if (fullName.value.trim() === "") {
            nameError.textContent = "Họ và tên không được để trống";
            fullName.style.marginBottom = "0";
            isValid = false;
        } else if (/\d/.test(fullName.value)) { // Kiểm tra nếu có số
            nameError.textContent = "Họ và tên không được chứa số";
            fullName.style.marginBottom = "0";
            isValid = false;
        }

        if (!/^([0-9]{10})$/.test(phone.value)) {
            phoneError.textContent = "Số điện thoại không hợp lệ";
            phone.style.marginBottom = "0";
            isValid = false;
        }

        if (!email.value.includes("@")) {
            emailError.textContent = "Email không hợp lệ";
            email.style.marginBottom = "0";
            isValid = false;
        }

        if (password.value.length < 6) {
            passError.textContent = "Mật khẩu phải có ít nhất 6 ký tự";
            password.style.marginBottom = "0";
            isValid = false;
        }

        if (password.value !== confirmPassword.value) {
            confirmPassError.textContent = "Mật khẩu nhập lại không khớp";
            confirmPassword.style.marginBottom = "0";
            isValid = false;
        }

        if (!isValid) {
            event.preventDefault();
        }
    });
});

