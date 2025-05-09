// Profile Module
const Profile = (function() {
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
            elements.avatarUpload.addEventListener('change', function() {
                if (this.files && this.files[0]) {
                    const reader = new FileReader();
                    reader.onload = function(e) {
                        elements.avatarImage.src = e.target.result;
                    }
                    reader.readAsDataURL(this.files[0]);
                }
            });
        }
    }

    // Profile Edit Functions
    function enableProfileEditMode(enable) {
        elements.profileDetailsSection.classList.toggle('edit-mode', enable);
        elements.fullNameInput.disabled = !enable;
        elements.phoneNumberInput.disabled = !enable;
        elements.emailInput.disabled = !enable;
        elements.addressInput.disabled = !enable;
        elements.avatarUpload.disabled = !enable;
        elements.avatarUploadBtn.classList.toggle('disabled', !enable);

        if (enable) {
            checkPhoneEmailChanges();
        } else {
            elements.verifyPhoneBtn.style.display = 'none';
            elements.verifyEmailBtn.style.display = 'none';
        }
    }

    function checkPhoneEmailChanges() {
        elements.phoneNumberInput.addEventListener('input', function() {
            if (this.value !== this.dataset.original) {
                elements.verifyPhoneBtn.style.display = 'block';
                delete this.dataset.verified;
            } else {
                elements.verifyPhoneBtn.style.display = 'none';
            }
        });

        elements.emailInput.addEventListener('input', function() {
            if (this.value !== this.dataset.original) {
                elements.verifyEmailBtn.style.display = 'block';
                delete this.dataset.verified;
            } else {
                elements.verifyEmailBtn.style.display = 'none';
            }
        });
    }

    // Password Functions
    function initPasswordSection() {
        elements.editPasswordBtn.addEventListener('click', function() {
            elements.passwordFields.style.display = 'block';
            elements.passwordButtons.style.display = 'flex';
            this.style.display = 'none';
            elements.currentPasswordInput.focus();
        });

        elements.savePasswordBtn.addEventListener('click', validateAndSavePassword);
        elements.cancelPasswordBtn.addEventListener('click', cancelPasswordChange);
    }

    function validateAndSavePassword() {
        if (!elements.currentPasswordInput.value) {
            alert('Vui lòng nhập mật khẩu hiện tại.');
            elements.currentPasswordInput.focus();
            return;
        }

        if (!elements.newPasswordInput.value) {
            alert('Vui lòng nhập mật khẩu mới.');
            elements.newPasswordInput.focus();
            return;
        }

        if (elements.newPasswordInput.value !== elements.confirmPasswordInput.value) {
            alert('Mật khẩu mới và xác nhận mật khẩu không khớp.');
            elements.confirmPasswordInput.focus();
            return;
        }

        const passwordRegex = new RegExp("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[\\@\\$\\!\\%\\*\\?\\&])[A-Za-z\\d\\@\\$\\!\\%\\*\\?\\&]{8,}$");
        if (!passwordRegex.test(elements.newPasswordInput.value)) {
            alert('Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.');
            elements.newPasswordInput.focus();
            return;
        }

        // Reset password fields
        elements.currentPasswordInput.value = '';
        elements.newPasswordInput.value = '';
        elements.confirmPasswordInput.value = '';

        // Hide password fields and buttons
        elements.passwordFields.style.display = 'none';
        elements.passwordButtons.style.display = 'none';
        elements.editPasswordBtn.style.display = 'block';

        // Show success message
        alert('Mật khẩu đã được cập nhật thành công!');
    }

    function cancelPasswordChange() {
        elements.currentPasswordInput.value = '';
        elements.newPasswordInput.value = '';
        elements.confirmPasswordInput.value = '';
        elements.passwordFields.style.display = 'none';
        elements.passwordButtons.style.display = 'none';
        elements.editPasswordBtn.style.display = 'block';
    }

    // OTP Functions
    function initOtpSection() {
        elements.verifyPhoneBtn.addEventListener('click', () => {
            verificationField = 'phone';
            verificationValue = elements.phoneNumberInput.value;
            elements.otpDestination.textContent = maskPhone(verificationValue);
            showOtpContainer();
            startOtpTimer();
        });

        elements.verifyEmailBtn.addEventListener('click', () => {
            verificationField = 'email';
            verificationValue = elements.emailInput.value;
            elements.otpDestination.textContent = maskEmail(verificationValue);
            showOtpContainer();
            startOtpTimer();
        });

        elements.cancelOtpBtn.addEventListener('click', () => {
            hideOtpContainer();
            resetOtpInputs();
            stopOtpTimer();
        });

        elements.verifyOtpBtn.addEventListener('click', verifyOtp);
        elements.resendOtpBtn.addEventListener('click', handleResendOtp);
        initOtpInputHandling();
    }

    function verifyOtp() {
        const otpValue = Array.from(elements.otpInputs).map(input => input.value).join('');
        
        if (otpValue.length === 6) {
            if (verificationField === 'phone') {
                elements.phoneNumberInput.dataset.original = elements.phoneNumberInput.value;
                elements.phoneNumberInput.dataset.verified = 'true';
                elements.verifyPhoneBtn.style.display = 'none';
            } else if (verificationField === 'email') {
                elements.emailInput.dataset.original = elements.emailInput.value;
                elements.emailInput.dataset.verified = 'true';
                elements.verifyEmailBtn.style.display = 'none';
            }
            
            hideOtpContainer();
            resetOtpInputs();
            stopOtpTimer();
            alert('Xác thực thành công!');
        } else {
            alert('Vui lòng nhập đủ 6 chữ số của mã OTP');
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
            input.addEventListener('keyup', function(e) {
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
            
            input.addEventListener('input', function() {
                this.value = this.value.replace(/[^0-9]/g, '');
            });
            
            input.addEventListener('paste', function(e) {
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
        elements.otpContainer.style.display = 'block';
        elements.otpInputs[0].focus();
    }

    function hideOtpContainer() {
        elements.otpContainer.style.display = 'none';
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
        
        elements.resendOtpBtn.disabled = true;
        
        otpTimer = setInterval(() => {
            timeLeft--;
            updateTimerDisplay();
            
            if (timeLeft <= 0) {
                stopOtpTimer();
                elements.resendOtpBtn.disabled = false;
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
        elements.otpTimerElement.textContent = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
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
            fullName: elements.fullNameInput.value,
            phoneNumber: elements.phoneNumberInput.value,
            email: elements.emailInput.value,
            address: elements.addressInput.value,
            avatar: elements.avatarImage.src
        };

        // Initialize sections
        initAvatarUpload();
        initPasswordSection();
        initOtpSection();

        // Profile edit event listeners
        elements.editProfileBtn.addEventListener('click', () => enableProfileEditMode(true));
        elements.saveProfileBtn.addEventListener('click', async function() {
            // Kiểm tra xác thực email và phone nếu đã thay đổi
            if (elements.phoneNumberInput.value !== elements.phoneNumberInput.dataset.original && !elements.phoneNumberInput.dataset.verified) {
                alert('Vui lòng xác thực số điện thoại mới trước khi lưu thông tin.');
                return;
            }
            
            if (elements.emailInput.value !== elements.emailInput.dataset.original && !elements.emailInput.dataset.verified) {
                alert('Vui lòng xác thực email mới trước khi lưu thông tin.');
                return;
            }

            // Tạo FormData để gửi cả file avatar nếu có
            const formData = new FormData();
            formData.append('FullName', elements.fullNameInput.value);
            formData.append('PhoneNumber', elements.phoneNumberInput.value);
            formData.append('Email', elements.emailInput.value);
            formData.append('Address', elements.addressInput.value);
            
            if (elements.avatarUpload.files.length > 0) {
                formData.append('AvatarFile', elements.avatarUpload.files[0]);
            }

            try {
                const response = await fetch('/User/UpdateProfile', {
                    method: 'POST',
                    body: formData // Gửi FormData thay vì JSON
                });

                const result = await response.json();
                if (result.success) {
                    // Cập nhật giá trị gốc
                    originalValues = {
                        fullName: elements.fullNameInput.value,
                        phoneNumber: elements.phoneNumberInput.value,
                        email: elements.emailInput.value,
                        address: elements.addressInput.value,
                        avatar: elements.avatarImage.src
                    };
                    
                    // Cập nhật dataset
                    elements.fullNameInput.dataset.original = elements.fullNameInput.value;
                    elements.phoneNumberInput.dataset.original = elements.phoneNumberInput.value;
                    elements.emailInput.dataset.original = elements.emailInput.value;
                    elements.addressInput.dataset.original = elements.addressInput.value;
                    
                    // Xóa trạng thái verified
                    delete elements.phoneNumberInput.dataset.verified;
                    delete elements.emailInput.dataset.verified;
                    
                    // Tắt chế độ chỉnh sửa
                    enableProfileEditMode(false);
                    
                    // Hiển thị thông báo thành công
                    alert('Thông tin tài khoản đã được cập nhật thành công!');
                    
                    // Reload trang để cập nhật thông tin mới
                    window.location.reload();
                } else {
                    alert(result.message || 'Có lỗi xảy ra khi cập nhật thông tin');
                }
            } catch (error) {
                console.error('Error:', error);
                alert('Có lỗi xảy ra khi cập nhật thông tin');
            }
        });
        elements.cancelProfileBtn.addEventListener('click', () => {
            elements.fullNameInput.value = originalValues.fullName;
            elements.phoneNumberInput.value = originalValues.phoneNumber;
            elements.emailInput.value = originalValues.email;
            elements.addressInput.value = originalValues.address;
            elements.avatarImage.src = originalValues.avatar;
            
            delete elements.phoneNumberInput.dataset.verified;
            delete elements.emailInput.dataset.verified;
            
            enableProfileEditMode(false);
        });
    }

    // Public API
    return {
        init: init
    };
})();

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', Profile.init); 

document.addEventListener('DOMContentLoaded', function() {
    // Các biến global
    let isEditing = false;
    let originalValues = {};
    let otpTimer;
    let otpTimeLeft = 120; // 2 phút

    // Elements
    const editProfileBtn = document.getElementById('editProfileBtn');
    const saveProfileBtn = document.getElementById('saveProfileBtn');
    const cancelProfileBtn = document.getElementById('cancelProfileBtn');
    const verifyEmailBtn = document.getElementById('verifyEmail');
    const verifyPhoneBtn = document.getElementById('verifyPhone');
    const otpContainer = document.getElementById('otpContainer');
    const otpInputs = document.querySelectorAll('.otp-input');
    const verifyOtpBtn = document.getElementById('verifyOtp');
    const cancelOtpBtn = document.getElementById('cancelOtp');
    const resendOtpBtn = document.getElementById('resendOtp');
    const otpTimerSpan = document.getElementById('otpTimer');
    const otpDestinationSpan = document.getElementById('otpDestination');

    // Form fields
    const formFields = {
        fullName: document.getElementById('fullName'),
        phoneNumber: document.getElementById('phoneNumber'),
        email: document.getElementById('email'),
        address: document.getElementById('address')
    };

    // Edit Profile
    editProfileBtn.addEventListener('click', function() {
        isEditing = true;
        toggleEditMode(true);
        saveOriginalValues();
    });

    // Cancel Edit
    cancelProfileBtn.addEventListener('click', function() {
        isEditing = false;
        toggleEditMode(false);
        restoreOriginalValues();
    });

    // Save Profile
    saveProfileBtn.addEventListener('click', async function() {
        if (!validateForm()) return;

        const formData = {
            user: {
                fullName: formFields.fullName.value,
                phoneNumber: formFields.phoneNumber.value,
                email: formFields.email.value,
                address: formFields.address.value
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
                showNotification('success', result.message);
                isEditing = false;
                toggleEditMode(false);
            } else {
                showNotification('error', result.message);
            }
        } catch (error) {
            showNotification('error', 'Có lỗi xảy ra khi cập nhật thông tin');
        }
    });

    // Verify Email
    verifyEmailBtn.addEventListener('click', async function() {
        const email = formFields.email.value;
        if (!email) {
            showNotification('error', 'Vui lòng nhập email');
            return;
        }

        try {
            const response = await fetch('/User/SendEmailVerificationOTP', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(email)
            });

            const result = await response.json();
            if (result.success) {
                showOTPContainer('email', email);
                startOTPTimer();
            } else {
                showNotification('error', result.message);
            }
        } catch (error) {
            showNotification('error', 'Có lỗi xảy ra khi gửi OTP');
        }
    });

    // Verify OTP
    verifyOtpBtn.addEventListener('click', async function() {
        const otp = Array.from(otpInputs).map(input => input.value).join('');
        if (otp.length !== 6) {
            showNotification('error', 'Vui lòng nhập đủ 6 ký tự OTP');
            return;
        }

        try {
            const response = await fetch('/User/VerifyEmailOTP', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(otp)
            });

            const result = await response.json();
            if (result.success) {
                showNotification('success', result.message);
                hideOTPContainer();
                stopOTPTimer();
            } else {
                showNotification('error', result.message);
            }
        } catch (error) {
            showNotification('error', 'Có lỗi xảy ra khi xác thực OTP');
        }
    });

    // OTP Input Handling
    otpInputs.forEach((input, index) => {
        input.addEventListener('keyup', function(e) {
            if (e.key >= '0' && e.key <= '9') {
                if (index < otpInputs.length - 1) {
                    otpInputs[index + 1].focus();
                }
            } else if (e.key === 'Backspace') {
                if (index > 0) {
                    otpInputs[index - 1].focus();
                }
            }
        });
    });

    // Helper Functions
    function toggleEditMode(enable) {
        Object.values(formFields).forEach(field => {
            field.disabled = !enable;
        });
        editProfileBtn.style.display = enable ? 'none' : 'block';
        document.querySelector('.edit-buttons').style.display = enable ? 'flex' : 'none';
    }

    function saveOriginalValues() {
        Object.entries(formFields).forEach(([key, field]) => {
            originalValues[key] = field.value;
        });
    }

    function restoreOriginalValues() {
        Object.entries(originalValues).forEach(([key, value]) => {
            formFields[key].value = value;
        });
    }

    function validateForm() {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        const phoneRegex = /^[0-9]{10,15}$/;

        if (!formFields.fullName.value.trim()) {
            showNotification('error', 'Vui lòng nhập họ tên');
            return false;
        }

        if (!phoneRegex.test(formFields.phoneNumber.value)) {
            showNotification('error', 'Số điện thoại không hợp lệ');
            return false;
        }

        if (!emailRegex.test(formFields.email.value)) {
            showNotification('error', 'Email không hợp lệ');
            return false;
        }

        return true;
    }

    function showOTPContainer(type, destination) {
        otpContainer.style.display = 'block';
        otpDestinationSpan.textContent = destination;
        document.getElementById('otpDescription').textContent = 
            `Chúng tôi đã gửi mã xác thực đến ${type === 'email' ? 'email' : 'số điện thoại'} của bạn. Vui lòng nhập mã để xác thực.`;
    }

    function hideOTPContainer() {
        otpContainer.style.display = 'none';
        otpInputs.forEach(input => input.value = '');
    }

    function startOTPTimer() {
        otpTimeLeft = 120;
        updateOTPTimer();
        otpTimer = setInterval(updateOTPTimer, 1000);
        resendOtpBtn.disabled = true;
    }

    function stopOTPTimer() {
        clearInterval(otpTimer);
        resendOtpBtn.disabled = false;
    }

    function updateOTPTimer() {
        const minutes = Math.floor(otpTimeLeft / 60);
        const seconds = otpTimeLeft % 60;
        otpTimerSpan.textContent = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
        
        if (otpTimeLeft <= 0) {
            stopOTPTimer();
            resendOtpBtn.disabled = false;
        } else {
            otpTimeLeft--;
        }
    }

    function showNotification(type, message) {
        // Implement your notification system here
        alert(message); // Temporary solution
    }
});