document.addEventListener("DOMContentLoaded", function () {
    const inputs = document.querySelectorAll(".otp-field input");
    const otpError = document.getElementById("otpError");

    inputs.forEach((input, index) => {
        input.dataset.index = index;
        input.addEventListener("keyup", handleOtp);
        input.addEventListener("paste", handleOnPasteOtp);
    });

    function handleOtp(e) {
        const input = e.target;
        let value = input.value;
        let isValidInput = value.match(/[0-9]/);
        input.value = isValidInput ? value[0] : "";

        let fieldIndex = input.dataset.index;
        if (fieldIndex < inputs.length - 1 && isValidInput) {
            input.nextElementSibling.focus();
        }

        if (e.key === "Backspace" && fieldIndex > 0) {
            input.previousElementSibling.focus();
        }

        if (fieldIndex == inputs.length - 1 && isValidInput) {
            submit(); // Tự động gửi OTP khi đủ 6 ký tự
        }
    }

    function handleOnPasteOtp(e) {
        const data = e.clipboardData.getData("text").trim();
        if (data.length === inputs.length && /^\d+$/.test(data)) {
            inputs.forEach((input, index) => (input.value = data[index]));
            submit();
        }
    }

    function submit() {
        let otp = "";
        inputs.forEach((input) => otp += input.value);

        if (otp.length === 6) {
            verifyOTP(otp);
        }
    }

    function verifyOTP(otp) {
        // otpError.textContent = "";

        fetch("/User/VerifyPasswordOTP", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ OTPPassCode: otp })
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    toastr.success("Xác thực tài khoản thành công!", "", {
                        timeOut: 4000,
                        extendedTimeOut: 1000,
                        closeButton: true,
                        progressBar: true,
                        positionClass: "toast-bottom-right"
                    });

                    setTimeout(() => {
                        window.location.href = data.redirectUrl;
                    }, 3000);
                } else {
                    // otpError.textContent = data.message;
                    toastr.error(data.message);
                    inputs.forEach(input => {
                        input.value = "";
                        input.disabled = false;
                    });
                    inputs[0].focus();
                }
            })
            .catch(error => {
                console.error("Lỗi khi gửi OTP:", error);
                // otpError.textContent = "⚠ Có lỗi xảy ra, vui lòng thử lại!";
                toastr.error("⚠ Có lỗi xảy ra, vui lòng thử lại!");
            });
    }
});

document.getElementById("resendOTP").addEventListener("click", function () {
    var email = document.getElementById("userEmail").value; // Lấy email người dùng
    var resendBtn = document.getElementById("resendOTP"); // Lấy nút resend
    var timeLeft = 60; // Đếm ngược từ 60 giây

    // Vô hiệu hóa nút và hiển thị countdown
    resendBtn.disabled = true;
    var countdown = setInterval(function () {
        if (timeLeft <= 0) {
            clearInterval(countdown);
            resendBtn.innerText = "Gửi lại OTP";
            resendBtn.disabled = false; // Kích hoạt lại nút
        } else {
            resendBtn.innerText = `Gửi lại OTP (${timeLeft}s)`;
            timeLeft--;
        }
    }, 1000);

    // Gửi yêu cầu Resend OTP
    fetch('/User/ResendPasswordOTP', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(email)
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                toastr.success(data.message, "Thành công", {
                    timeOut: 3000,
                    closeButton: true,
                    progressBar: true,
                    positionClass: "toast-bottom-right"
                });
            } else {
                toastr.error(data.message, "Lỗi", {
                    timeOut: 3000,
                    closeButton: true,
                    progressBar: true,
                    positionClass: "toast-bottom-right"
                });

                // Nếu có lỗi, cho phép gửi lại ngay lập tức
                clearInterval(countdown);
                resendBtn.innerText = "Gửi lại OTP";
                resendBtn.disabled = false;
            }
        })
        .catch(error => {
            console.error("Lỗi khi gửi lại OTP:", error);
            toastr.error("Đã xảy ra lỗi khi gửi lại OTP!", "Lỗi", {
                timeOut: 3000,
                closeButton: true,
                progressBar: true
            });

            // Nếu có lỗi, cho phép gửi lại ngay lập tức
            clearInterval(countdown);
            resendBtn.innerText = "Gửi lại OTP";
            resendBtn.disabled = false;
        });
});
