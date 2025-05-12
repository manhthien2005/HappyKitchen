document.addEventListener('DOMContentLoaded', function () {
    let timeLeft = 10;
    const countdownElement = document.getElementById("countdown");
    const progressBar = document.getElementById("progress-bar");

    // Thiết lập thanh tiến trình ban đầu
    progressBar.style.width = "100%";

    const timer = setInterval(() => {
        timeLeft--;
        countdownElement.textContent = timeLeft;

        // Cập nhật thanh tiến trình
        progressBar.style.width = (timeLeft / 10 * 100) + "%";

        if (timeLeft <= 0) {
            clearInterval(timer);
            window.location.href = "/Home/Index"; // Chuyển hướng về trang chủ
        }
    }, 1000);

    setTimeout(() => {
        createStarBurstEffect();
    }, 500);
});

function createStarBurstEffect() {
    // Xóa các ngôi sao cũ (nếu có)
    const container = document.getElementById("celebration-container");
    container.innerHTML = '';

    // Lấy kích thước của celebration-container
    const card = document.getElementById("celebration-container");
    const cardRect = card.getBoundingClientRect();
    const centerX = cardRect.width / 2;
    const centerY = cardRect.height / 2;

    // Debug: Kiểm tra giá trị
    console.log('cardRect:', cardRect, 'centerX:', centerX, 'centerY:', centerY);

    // Màu sắc theme vàng
    const colors = [
        "#D6B981", // Vàng chính
        "#E5D3A8", // Vàng nhạt
        "#B39B69", // Vàng đậm
        "#FFD700", // Gold
        "#FFFFFF"  // Trắng
    ];

    // Số lượng ngôi sao và tốc độ mặc định
    const totalStars = 80;
    const speedFactor = 2.5;

    // Tạo các ngôi sao bắn ra
    for (let i = 0; i < totalStars; i++) {
        setTimeout(() => {
            // Tạo phần tử ngôi sao
            const star = document.createElement("div");
            star.className = "celebration-star";

            // Chọn ngẫu nhiên màu sắc
            const color = colors[Math.floor(Math.random() * colors.length)];

            // Thiết lập vị trí ban đầu (trung tâm thẻ)
            star.style.left = `${centerX}px`;
            star.style.top = `${centerY}px`;

            // Thiết lập màu sắc
            star.style.color = color;

            // Thêm ký tự ngôi sao
            star.innerHTML = "✦";

            // Thiết lập kích thước ngẫu nhiên
            const size = 12 + Math.random() * 20;
            star.style.fontSize = `${size}px`;

            // Thiết lập animation
            const animDuration = (1.5 + Math.random() * 2.5) / speedFactor;
            const animDelay = Math.random() * 0.7 / speedFactor;

            // Thiết lập hướng bay ngẫu nhiên
            const angle = Math.random() * Math.PI * 2;
            const distance = 100 + Math.random() * 400;
            const translateX = Math.cos(angle) * distance;
            const translateY = Math.sin(angle) * distance;

            // Số vòng xoay ngẫu nhiên
            const rotations = 3 + Math.floor(Math.random() * 5);

            // Thiết lập animation với CSS
            star.style.animation = `starMoveAndRotate ${animDuration}s ease-out ${animDelay}s forwards`;

            // Thiết lập vị trí cuối cùng và xoay
            star.style.setProperty("--translateX", `${translateX}px`);
            star.style.setProperty("--translateY", `${translateY}px`);
            star.style.setProperty("--rotations", `${rotations * 360}deg`);

            // Thêm vào container
            container.appendChild(star);

            // Debug: Kiểm tra ngôi sao được thêm
            console.log('Star added:', star, 'Animation:', star.style.animation);

            // Xóa sau khi animation kết thúc
            setTimeout(() => {
                star.remove();
            }, (animDuration + animDelay) * 1000 + 100);
        }, i * 10 / speedFactor);
    }

    // Tạo hiệu ứng lấp lánh bổ sung
    for (let i = 0; i < 40; i++) {
        setTimeout(() => {
            const sparkle = document.createElement("div");
            sparkle.className = "celebration-sparkle";

            // Vị trí ngẫu nhiên trong thẻ
            const randomX = Math.random() * cardRect.width;
            const randomY = Math.random() * cardRect.height;

            sparkle.style.left = `${randomX}px`;
            sparkle.style.top = `${randomY}px`;
            sparkle.style.backgroundColor = colors[Math.floor(Math.random() * colors.length)];

            // Thêm vào container
            container.appendChild(sparkle);

            // Hiệu ứng lấp lánh
            sparkle.style.animation = `sparkle 0.6s ease-in-out`;
            setTimeout(() => {
                sparkle.remove();
            }, 600);
        }, i * 200 / speedFactor + Math.random() * 2000 / speedFactor);
    }
}