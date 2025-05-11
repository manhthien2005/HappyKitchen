document.addEventListener('DOMContentLoaded', function() {
    const avatarImg = document.querySelector('.avatar-img');
    const dropdownMenu = document.querySelector('.dropdown-menu');
    
    if (avatarImg && dropdownMenu) {
        // Toggle dropdown khi click vào avatar
        avatarImg.addEventListener('click', function(e) {
            e.stopPropagation();
            dropdownMenu.classList.toggle('show');
        });

        // Đóng dropdown khi click ra ngoài
        document.addEventListener('click', function(e) {
            if (!dropdownMenu.contains(e.target) && !avatarImg.contains(e.target)) {
                dropdownMenu.classList.remove('show');
            }
        });

        // Xử lý hover effect
        avatarImg.addEventListener('mouseenter', function() {
            this.style.transform = 'scale(1.05)';
        });

        avatarImg.addEventListener('mouseleave', function() {
            this.style.transform = 'scale(1)';
        });
    }
}); 