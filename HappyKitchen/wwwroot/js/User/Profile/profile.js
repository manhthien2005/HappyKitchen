// Profile Module
const Profile = (function () {
    // Private variables
    let verificationField = null;
    let verificationValue = null;
    let otpTimer = null;
    let timeLeft = 120; // 2 minutes
    let originalValues = {};

    // DOM Elements
    const elements = {
        profileForm: document.getElementById('profileForm'),
        avatarUpload: document.getElementById('avatarUpload'),
        avatarImage: document.getElementById('avatarPreview'),
        avatarUploadBtn: document.getElementById('avatarUploadBtn'),
        editProfileBtn: document.getElementById('editProfileBtn'),
        saveProfileBtn: document.getElementById('saveProfileBtn'),
        cancelProfileBtn: document.getElementById('cancelProfileBtn'),
        profileDetailsSection: document.querySelector('.profile-details-section'),
        fullNameInput: document.getElementById('fullName'),
        phoneNumberInput: document.getElementById('phoneNumber'),
        emailInput: document.getElementById('email'),
        addressInput: document.getElementById('address'),
        verifyPhoneBtn: document.getElementById('verifyPhone'),
        verifyEmailBtn: document.getElementById('verifyEmail'),
        editPasswordBtn: document.getElementById('editPasswordBtn'),
        savePasswordBtn: document.getElementById('savePasswordBtn'),
        cancelPasswordBtn: document.getElementById('cancelPasswordBtn'),
        passwordFields: document.getElementById('passwordFields'),
        passwordButtons: document.getElementById('passwordButtons'),
        currentPasswordInput: document.getElementById('currentPassword'),
        newPasswordInput: document.getElementById('newPassword'),
        confirmPasswordInput: document.getElementById('confirmPassword'),
        otpContainer: document.getElementById('otpContainer'),
        otpDestination: document.getElementById('otpDestination'),
        otpTimerElement: document.getElementById('otpTimer'),
        resendOtpBtn: document.getElementById('resendOtp'),
        verifyOtpBtn: document.getElementById('verifyOtp'),
        cancelOtpBtn: document.getElementById('cancelOtp'),
        otpInputs: document.querySelectorAll('.otp-input')
    };

    // Avatar Functions
    function initAvatarUpload() {
        if (elements.avatarUpload && elements.avatarImage) {
            elements.avatarUpload.addEventListener('change', function () {
                if (this.files && this.files[0]) {
                    const reader = new FileReader();
                    reader.onload = function (e) {
                        elements.avatarImage.src = e.target.result;
                    }
                    reader.readAsDataURL(this.files[0]);
                }
            });
        }
    }

    // Profile Edit Functions
    function enableProfileEditMode(enable) {
        if (elements.profileDetailsSection) {
            elements.profileDetailsSection.classList.toggle('edit-mode', enable);
        }
        if (elements.fullNameInput) elements.fullNameInput.disabled = !enable;
        if (elements.phoneNumberInput) elements.phoneNumberInput.disabled = !enable;
        if (elements.emailInput) elements.emailInput.disabled = !enable;
        if (elements.addressInput) elements.addressInput.disabled = !enable;
        if (elements.avatarUpload) elements.avatarUpload.disabled = !enable;
        if (elements.avatarUploadBtn) elements.avatarUploadBtn.classList.toggle('disabled', !enable);

        if (enable) {
            checkPhoneEmailChanges();
        } else {
            if (elements.verifyPhoneBtn) elements.verifyPhoneBtn.style.display = 'none';
            if (elements.verifyEmailBtn) elements.verifyEmailBtn.style.display = 'none';
        }
    }

    function checkPhoneEmailChanges() {
        if (elements.phoneNumberInput) {
            elements.phoneNumberInput.addEventListener('input', function () {
                if (this.value !== this.dataset.original) {
                    if (elements.verifyPhoneBtn) elements.verifyPhoneBtn.style.display = 'block';
                    delete this.dataset.verified;
                } else {
                    if (elements.verifyPhoneBtn) elements.verifyPhoneBtn.style.display = 'none';
                }
            });
        }

        if (elements.emailInput) {
            elements.emailInput.addEventListener('input', function () {
                if (this.value !== this.dataset.original) {
                    if (elements.verifyEmailBtn) elements.verifyEmailBtn.style.display = 'block';
                    delete this.dataset.verified;
                } else {
                    if (elements.verifyEmailBtn) elements.verifyEmailBtn.style.display = 'none';
                }
            });
        }
    }

    // Password Functions
    function initPasswordSection() {
        if (elements.editPasswordBtn) {
            elements.editPasswordBtn.addEventListener('click', function () {
                if (elements.passwordFields) elements.passwordFields.style.display = 'block';
                if (elements.passwordButtons) elements.passwordButtons.style.display = 'flex';
                this.style.display = 'none';
                if (elements.currentPasswordInput) elements.currentPasswordInput.focus();
            });
        }

        if (elements.savePasswordBtn) {
            elements.savePasswordBtn.addEventListener('click', validateAndSavePassword);
        }
        if (elements.cancelPasswordBtn) {
            elements.cancelPasswordBtn.addEventListener('click', cancelPasswordChange);
        }
    }

    function validateAndSavePassword() {
        if (!elements.currentPasswordInput.value) {
            if (typeof toastr !== 'undefined') {
                toastr.error('Vui lòng nhập mật khẩu hiện tại.');
            } else {
                console.error('toastr chưa được tải!');
                alert('Vui lòng nhập mật khẩu hiện tại.');
            }
            if (elements.currentPasswordInput) elements.currentPasswordInput.focus();
            return;
        }

        if (!elements.newPasswordInput.value) {
            if (typeof toastr !== 'undefined') {
                toastr.error('Vui lòng nhập mật khẩu mới.');
            } else {
                console.error('toastr chưa được tải!');
                alert('Vui lòng nhập mật khẩu mới.');
            }
            if (elements.newPasswordInput) elements.newPasswordInput.focus();
            return;
        }

        if (elements.newPasswordInput.value !== elements.confirmPasswordInput.value) {
            if (typeof toastr !== 'undefined') {
                toastr.error('Mật khẩu mới và xác nhận mật khẩu không khớp.');
            } else {
                console.error('toastr chưa được tải!');
                alert('Mật khẩu mới và xác nhận mật khẩu không khớp.');
            }
            if (elements.confirmPasswordInput) elements.confirmPasswordInput.focus();
            return;
        }

        const passwordRegex = new RegExp("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[\\@\\$\\!\\%\\*\\?\\&])[A-Za-z\\d\\@\\$\\!\\%\\*\\?\\&]{8,}$");
        if (!passwordRegex.test(elements.newPasswordInput.value)) {
            if (typeof toastr !== 'undefined') {
                toastr.error('Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.');
            } else {
                console.error('toastr chưa được tải!');
                alert('Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.');
            }
            if (elements.newPasswordInput) elements.newPasswordInput.focus();
            return;
        }

        // Gọi API cập nhật mật khẩu
        const formData = {
            currentPassword: elements.currentPasswordInput.value,
            newPassword: elements.newPasswordInput.value,
            confirmPassword: elements.confirmPasswordInput.value
        };

        fetch('/User/ChangePassword', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(formData)
        })
        .then(response => response.json())
        .then(result => {
            if (result.success) {
                // Reset password fields
                if (elements.currentPasswordInput) elements.currentPasswordInput.value = '';
                if (elements.newPasswordInput) elements.newPasswordInput.value = '';
                if (elements.confirmPasswordInput) elements.confirmPasswordInput.value = '';

                // Hide password fields and buttons
                if (elements.passwordFields) elements.passwordFields.style.display = 'none';
                if (elements.passwordButtons) elements.passwordButtons.style.display = 'none';
                if (elements.editPasswordBtn) elements.editPasswordBtn.style.display = 'block';

                // Show success message
                if (typeof toastr !== 'undefined') {
                    toastr.success('Mật khẩu đã được cập nhật thành công!');
                } else {
                    console.error('toastr chưa được tải!');
                    alert('Mật khẩu đã được cập nhật thành công!');
                }
            } else {
                if (typeof toastr !== 'undefined') {
                    toastr.error(result.message || 'Có lỗi xảy ra khi cập nhật mật khẩu');
                } else {
                    console.error('toastr chưa được tải!');
                    alert(result.message || 'Có lỗi xảy ra khi cập nhật mật khẩu');
                }
            }
        })
        .catch(error => {
            console.error('Error:', error);
            if (typeof toastr !== 'undefined') {
                toastr.error('Có lỗi xảy ra khi cập nhật mật khẩu');
            } else {
                console.error('toastr chưa được tải!');
                alert('Có lỗi xảy ra khi cập nhật mật khẩu');
            }
        });
    }

    function cancelPasswordChange() {
        if (elements.currentPasswordInput) elements.currentPasswordInput.value = '';
        if (elements.newPasswordInput) elements.newPasswordInput.value = '';
        if (elements.confirmPasswordInput) elements.confirmPasswordInput.value = '';
        if (elements.passwordFields) elements.passwordFields.style.display = 'none';
        if (elements.passwordButtons) elements.passwordButtons.style.display = 'none';
        if (elements.editPasswordBtn) elements.editPasswordBtn.style.display = 'block';
    }

    // OTP Functions
    function initOtpSection() {
        if (elements.verifyPhoneBtn) {
            elements.verifyPhoneBtn.addEventListener('click', () => {
                verificationField = 'phone';
                verificationValue = elements.phoneNumberInput.value;
                if (elements.otpDestination) elements.otpDestination.textContent = maskPhone(verificationValue);
                showOtpContainer();
                startOtpTimer();
            });
        }

        if (elements.verifyEmailBtn) {
            elements.verifyEmailBtn.addEventListener('click', async () => {
                verificationField = 'email';
                verificationValue = elements.emailInput.value;
                
                try {
                    const response = await fetch('/User/SendEmailVerificationOTP', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(verificationValue)
                    });

                    const result = await response.json();
                    
                    if (result.success) {
                        if (elements.otpDestination) elements.otpDestination.textContent = maskEmail(verificationValue);
                        showOtpContainer();
                        startOtpTimer();
                    } else {
                        if (typeof toastr !== 'undefined') {
                            toastr.error(result.message || 'Không thể gửi mã OTP');
                        } else {
                            alert(result.message || 'Không thể gửi mã OTP');
                        }
                    }
                } catch (error) {
                    console.error('Error sending OTP:', error);
                    if (typeof toastr !== 'undefined') {
                        toastr.error('Có lỗi xảy ra khi gửi mã OTP');
                    } else {
                        alert('Có lỗi xảy ra khi gửi mã OTP');
                    }
                }
            });
        }

        if (elements.cancelOtpBtn) {
            elements.cancelOtpBtn.addEventListener('click', () => {
                hideOtpContainer();
                resetOtpInputs();
                stopOtpTimer();
            });
        }

        if (elements.verifyOtpBtn) {
            elements.verifyOtpBtn.addEventListener('click', verifyOtp);
        }
        if (elements.resendOtpBtn) {
            elements.resendOtpBtn.addEventListener('click', handleResendOtp);
        }
        initOtpInputHandling();
    }

    function verifyOtp() {
        const otpValue = Array.from(elements.otpInputs).map(input => input.value).join('');

        if (otpValue.length === 6) {
            if (verificationField === 'phone') {
                elements.phoneNumberInput.dataset.original = elements.phoneNumberInput.value;
                elements.phoneNumberInput.dataset.verified = 'true';
                if (elements.verifyPhoneBtn) elements.verifyPhoneBtn.style.display = 'none';
            } else if (verificationField === 'email') {
                elements.emailInput.dataset.original = elements.emailInput.value;
                elements.emailInput.dataset.verified = 'true';
                if (elements.verifyEmailBtn) elements.verifyEmailBtn.style.display = 'none';
            }

            hideOtpContainer();
            resetOtpInputs();
            stopOtpTimer();
            if (typeof toastr !== 'undefined') {
                toastr.success('Xác thực thành công!');
            } else {
                console.error('toastr chưa được tải!');
                alert('Xác thực thành công!');
            }
        } else {
            if (typeof toastr !== 'undefined') {
                toastr.error('Vui lòng nhập đủ 6 chữ số của mã OTP');
            } else {
                console.error('toastr chưa được tải!');
                alert('Vui lòng nhập đủ 6 chữ số của mã OTP');
            }
        }
    }

    function handleResendOtp() {
        if (!elements.resendOtpBtn.disabled) {
            resetOtpInputs();
            startOtpTimer();
        }
    }

    function initOtpInputHandling() {
        elements.otpInputs.forEach(input => {
            input.addEventListener('keyup', function (e) {
                if (this.value.length === 1) {
                    const nextIndex = parseInt(this.dataset.index) + 1;
                    const nextInput = document.querySelector(`.otp-input[data-index="${nextIndex}"]`);
                    if (nextInput) {
                        nextInput.focus();
                    }
                }

                if (e.key === 'Backspace' && this.value.length === 0) {
                    const prevIndex = parseInt(this.dataset.index) - 1;
                    const prevInput = document.querySelector(`.otp-input[data-index="${prevIndex}"]`);
                    if (prevInput) {
                        prevInput.focus();
                    }
                }
            });

            input.addEventListener('input', function () {
                this.value = this.value.replace(/[^0-9]/g, '');
            });

            input.addEventListener('paste', function (e) {
                e.preventDefault();
                const pastedData = (e.clipboardData || window.clipboardData).getData('text');
                const digits = pastedData.replace(/[^0-9]/g, '').split('');

                if (digits.length > 0) {
                    elements.otpInputs.forEach((input, index) => {
                        if (index < digits.length) {
                            input.value = digits[index];
                        }
                    });

                    const lastFilledIndex = Math.min(digits.length, elements.otpInputs.length);
                    if (lastFilledIndex < elements.otpInputs.length) {
                        elements.otpInputs[lastFilledIndex].focus();
                    } else {
                        elements.otpInputs[elements.otpInputs.length - 1].focus();
                    }
                }
            });
        });
    }

    // Helper Functions
    function showOtpContainer() {
        if (elements.otpContainer) elements.otpContainer.style.display = 'block';
        if (elements.otpInputs[0]) elements.otpInputs[0].focus();
    }

    function hideOtpContainer() {
        if (elements.otpContainer) elements.otpContainer.style.display = 'none';
        verificationField = null;
        verificationValue = null;
    }

    function resetOtpInputs() {
        elements.otpInputs.forEach(input => {
            input.value = '';
        });
    }

    function startOtpTimer() {
        timeLeft = 120;
        updateTimerDisplay();

        if (otpTimer) {
            clearInterval(otpTimer);
        }

        if (elements.resendOtpBtn) elements.resendOtpBtn.disabled = true;

        otpTimer = setInterval(() => {
            timeLeft--;
            updateTimerDisplay();

            if (timeLeft <= 0) {
                stopOtpTimer();
                if (elements.resendOtpBtn) elements.resendOtpBtn.disabled = false;
            }
        }, 1000);
    }

    function stopOtpTimer() {
        if (otpTimer) {
            clearInterval(otpTimer);
            otpTimer = null;
        }
    }

    function updateTimerDisplay() {
        const minutes = Math.floor(timeLeft / 60);
        const seconds = timeLeft % 60;
        if (elements.otpTimerElement) {
            elements.otpTimerElement.textContent = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
        }
    }

    function maskPhone(phone) {
        if (phone.length <= 4) return phone;
        return phone.slice(0, 3) + '*'.repeat(phone.length - 7) + phone.slice(-4);
    }

    function maskEmail(email) {
        const parts = email.split('@');
        if (parts.length !== 2) return email;

        const name = parts[0];
        const domain = parts[1];

        let maskedName = name;
        if (name.length > 2) {
            maskedName = name.slice(0, 2) + '*'.repeat(name.length - 2);
        }

        return maskedName + '@' + domain;
    }

    // Initialize
    function init() {
        // Store original values
        originalValues = {
            fullName: elements.fullNameInput ? elements.fullNameInput.value : '',
            phoneNumber: elements.phoneNumberInput ? elements.phoneNumberInput.value : '',
            email: elements.emailInput ? elements.emailInput.value : '',
            address: elements.addressInput ? elements.addressInput.value : '',
            avatar: elements.avatarImage ? elements.avatarImage.src : ''
        };

        // Initialize sections
        initAvatarUpload();
        initPasswordSection();
        initOtpSection();

        // Profile edit event listeners
        if (elements.editProfileBtn) {
            elements.editProfileBtn.addEventListener('click', () => enableProfileEditMode(true));
        }
        if (elements.saveProfileBtn) {
            elements.saveProfileBtn.addEventListener('click', async function () {
                // Kiểm tra xác thực email và phone nếu đã thay đổi
                if (elements.phoneNumberInput && elements.phoneNumberInput.value !== elements.phoneNumberInput.dataset.original && !elements.phoneNumberInput.dataset.verified) {
                    if (typeof toastr !== 'undefined') {
                        toastr.error('Vui lòng xác thực số điện thoại mới trước khi lưu thông tin.');
                    } else {
                        console.error('toastr chưa được tải!');
                        alert('Vui lòng xác thực số điện thoại mới trước khi lưu thông tin.');
                    }
                    return;
                }

                if (elements.emailInput && elements.emailInput.value !== elements.emailInput.dataset.original && !elements.emailInput.dataset.verified) {
                    if (typeof toastr !== 'undefined') {
                        toastr.error('Vui lòng xác thực email mới trước khi lưu thông tin.');
                    } else {
                        console.error('toastr chưa được tải!');
                        alert('Vui lòng xác thực email mới trước khi lưu thông tin.');
                    }
                    return;
                }

                const formData = {
                    user: {
                        FullName: elements.fullNameInput ? elements.fullNameInput.value : '',
                        PhoneNumber: elements.phoneNumberInput ? elements.phoneNumberInput.value : '',
                        Email: elements.emailInput ? elements.emailInput.value : '',
                        Address: elements.addressInput ? elements.addressInput.value : ''
                    }
                };

                try {
                    const response = await fetch('/User/UpdateProfile', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(formData)
                    });

                    const result = await response.json();
                    if (result.success) {
                        if (typeof toastr !== 'undefined') {
                            toastr.success('Thông tin tài khoản đã được cập nhật thành công!');
                        } else {
                            console.error('toastr chưa được tải!');
                            alert('Thông tin tài khoản đã được cập nhật thành công!');
                        }
                        originalValues = {
                            fullName: elements.fullNameInput ? elements.fullNameInput.value : '',
                            phoneNumber: elements.phoneNumberInput ? elements.phoneNumberInput.value : '',
                            email: elements.emailInput ? elements.emailInput.value : '',
                            address: elements.addressInput ? elements.addressInput.value : '',
                            avatar: elements.avatarImage ? elements.avatarImage.src : ''
                        };
                        enableProfileEditMode(false);
                        if (elements.editPasswordBtn) elements.editPasswordBtn.style.display = 'block'; // Đảm bảo nút đổi mật khẩu hiển thị
                    } else {
                        if (typeof toastr !== 'undefined') {
                            toastr.error(result.message || 'Có lỗi xảy ra khi cập nhật thông tin');
                        } else {
                            console.error('toastr chưa được tải!');
                            alert(result.message || 'Có lỗi xảy ra khi cập nhật thông tin');
                        }
                    }
                } catch (error) {
                    console.error('Error:', error);
                    if (typeof toastr !== 'undefined') {
                        toastr.error('Có lỗi xảy ra khi cập nhật thông tin');
                    } else {
                        console.error('toastr chưa được tải!');
                        alert('Có lỗi xảy ra khi cập nhật thông tin');
                    }
                }
            });
        }
        if (elements.cancelProfileBtn) {
            elements.cancelProfileBtn.addEventListener('click', () => {
                if (elements.fullNameInput) elements.fullNameInput.value = originalValues.fullName;
                if (elements.phoneNumberInput) elements.phoneNumberInput.value = originalValues.phoneNumber;
                if (elements.emailInput) elements.emailInput.value = originalValues.email;
                if (elements.addressInput) elements.addressInput.value = originalValues.address;
                if (elements.avatarImage) elements.avatarImage.src = originalValues.avatar;

                if (elements.phoneNumberInput) delete elements.phoneNumberInput.dataset.verified;
                if (elements.emailInput) delete elements.emailInput.dataset.verified;

                enableProfileEditMode(false);
            });
        }
    }

    // Public API
    return {
        init: init
    };
})();

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', Profile.init);