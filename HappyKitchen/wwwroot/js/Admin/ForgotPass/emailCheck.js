$(document).ready(function () {
    $("form").off("submit").on("submit", async function (e) { // Ngăn trùng sự kiện
        e.preventDefault(); // Ngăn form submit mặc định

        let email = $("input[type='email']").val().trim().toLowerCase(); // Chuẩn hóa email
        console.log("Email đăng nhập: " + email);

        if (!email) {
            toastr.error("Vui lòng nhập email!");
            return;
        }

        let submitBtn = $("button[type='submit']");
        if (submitBtn.prop("disabled")) return; // Ngăn spam click

        submitBtn.prop("disabled", true).text("Đang kiểm tra...");

        try {
            let checkEmailResponse = await $.ajax({
                url: "/Admin/CheckEmailExists",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(email) // Gửi email dạng string
            });

            if (!checkEmailResponse) { // Vì controller trả về true/false trực tiếp
                toastr.error("Email không tồn tại trong hệ thống!");
                submitBtn.prop("disabled", false).text("Xác nhận");
                return;
            }

            // **Gửi OTP với POST**
            let sendOtpResponse = await $.ajax({
                url: "/Admin/SendPasswordOTP",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(email) // Gửi email đúng format JSON
            });

            // Chuyển hướng đến trang Verify OTP nếu thành công
            if (sendOtpResponse.success) {
                window.location.href = "/Admin/VerifyPasswordOTP?email=" + encodeURIComponent(email);
            } else {
                toastr.error("Không thể gửi OTP, vui lòng thử lại!");
                submitBtn.prop("disabled", false).text("Xác nhận");
            }

        } catch (error) {
            toastr.error("Lỗi kết nối đến server.");
            submitBtn.prop("disabled", false).text("Xác nhận");
        }
    });
});
