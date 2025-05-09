document.addEventListener('DOMContentLoaded', function() {
    // Xử lý upload avatar
    const avatarUploadBtn = document.querySelector('.avatar-upload-btn');
    const avatarInput = document.querySelector('#avatar-input');
    
    if (avatarUploadBtn && avatarInput) {
        avatarUploadBtn.addEventListener('click', function() {
            avatarInput.click();
        });

        avatarInput.addEventListener('change', function(e) {
            if (e.target.files && e.target.files[0]) {
                const reader = new FileReader();
                reader.onload = function(e) {
                    document.querySelector('.avatar-image').src = e.target.result;
                }
                reader.readAsDataURL(e.target.files[0]);
            }
        });
    }

    // Xử lý OTP verification
    const otpInputs = document.querySelectorAll('.otp-input');
    const verifyBtn = document.querySelector('.otp-verify-btn');
    const resendBtn = document.querySelector('.otp-resend');
    let timer = 60;
    let timerInterval;

    if (otpInputs.length > 0) {
        // Auto focus next input
        otpInputs.forEach((input, index) => {
            input.addEventListener('input', function() {
                if (this.value.length === 1) {
                    if (index < otpInputs.length - 1) {
                        otpInputs[index + 1].focus();
                    }
                }
            });

            input.addEventListener('keydown', function(e) {
                if (e.key === 'Backspace' && !this.value && index > 0) {
                    otpInputs[index - 1].focus();
                }
            });
        });

        // Start timer
        if (resendBtn) {
            startTimer();
        }
    }

    function startTimer() {
        resendBtn.disabled = true;
        timerInterval = setInterval(() => {
            timer--;
            document.querySelector('.otp-timer').textContent = `${timer}s`;
            
            if (timer <= 0) {
                clearInterval(timerInterval);
                resendBtn.disabled = false;
                timer = 60;
            }
        }, 1000);
    }

    // Verify OTP
    if (verifyBtn) {
        verifyBtn.addEventListener('click', function() {
            const otp = Array.from(otpInputs).map(input => input.value).join('');
            // Add your OTP verification logic here
            console.log('Verifying OTP:', otp);
        });
    }

    // Resend OTP
    if (resendBtn) {
        resendBtn.addEventListener('click', function() {
            if (!this.disabled) {
                startTimer();
                // Add your resend OTP logic here
                console.log('Resending OTP...');
            }
        });
    }
}); 