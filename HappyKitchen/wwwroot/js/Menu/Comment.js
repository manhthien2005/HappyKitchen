document.addEventListener('DOMContentLoaded', function () {
    let currentPage = 1;
    const commentsPerPage = 5;
    const commentList = document.getElementById('comment-list');
    const prevPageBtn = document.getElementById('prev-page');
    const nextPageBtn = document.getElementById('next-page');
    const totalComments = document.getElementById('comment-count');
    const currentPageDisplay = document.getElementById('current-page');
    const totalPagesDisplay = document.getElementById('total-pages');
    let selectedRating = 0;

    document.querySelectorAll('input[name="rating"]').forEach(function (input) {
        input.addEventListener('change', function () {
            selectedRating = parseInt(this.value);
        });
    });

    function updatePagination() {
        const totalPages = Math.ceil(allComments.length / commentsPerPage);
        currentPageDisplay.textContent = currentPage;
        totalPagesDisplay.textContent = totalPages;
        prevPageBtn.disabled = currentPage === 1;
        nextPageBtn.disabled = currentPage >= totalPages || totalPages === 0;
    }

    function generateStars(rating) {
        let starsHtml = "";
        // Lặp qua 5 ngôi sao
        for (let i = 1; i <= 5; i++) {
            let fillPercentage = 0;
            if (i <= Math.floor(rating)) {
                // Ngôi sao đầy 100%
                fillPercentage = 100;
            } else if (i === Math.ceil(rating) && (rating % 1) !== 0) {
                // Ngôi sao có phần đầy theo phần thập phân
                fillPercentage = (rating % 1) * 100;
            }
            // Tạo HTML cho mỗi ngôi sao
            let starHtml = '<div class="star">★';
            if (fillPercentage > 0) {
                starHtml += `<div class="star-fill" style="width: ${fillPercentage}%;">★</div>`;
            }
            starHtml += '</div>';
            starsHtml += starHtml;
        }
        return starsHtml;
    }

    // Hàm chuyển đổi chuỗi dd/MM/yyyy sang đối tượng Date
    function parseDate(dateString) {
        const parts = dateString.split('/');
        // parts[0] = dd, parts[1] = MM, parts[2] = yyyy
        return new Date(parts[2], parts[1] - 1, parts[0]);
    }

    // Hàm sắp xếp bình luận theo ngày giảm dần (mới nhất ở đầu)
    function sortCommentsByDateDesc() {
        allComments.sort((a, b) => {
            return parseDate(b.date) - parseDate(a.date);
        });
    }

    function displayComments() {
        // Làm mới danh sách bình luận trước khi render lại
        commentList.innerHTML = "";

        sortCommentsByDateDesc();

        // Render lại các bình luận từ mảng allComments
        allComments.slice((currentPage - 1) * commentsPerPage, currentPage * commentsPerPage)
            .forEach(comment => {
                const starRating = Number(comment.rating);
                const commentElement = document.createElement('div');
                commentElement.className = 'comment';
                commentElement.innerHTML = `
                <div class="comment-header">
                    <span class="comment-author">${comment.author}</span>
                    <span class="comment-date">${comment.date}</span>
                </div>
                <div style="margin-bottom: 10px;" class="rating-stars">${generateStars(starRating)}</div>
                <div class="comment-text">${comment.text}</div>
            `;
                commentList.appendChild(commentElement);
            });
    }


    function updateCommentCount() {
        totalComments.textContent = `${allComments.length} comments`;
    }

    function updateRatingDisplay() {
        // Tính số lượng đánh giá dựa trên mảng allComments
        const ratingCount = allComments.length;

        // Tính trung bình rating
        if (ratingCount === 0) {
            document.getElementById('average-rating').textContent = "0.0";
        } else {
            const totalRating = allComments.reduce((sum, comment) => sum + Number(comment.rating), 0);
            const avgRating = (totalRating / ratingCount).toFixed(1);
            document.getElementById('average-rating').textContent = avgRating;
        }

        // Cập nhật số lượng đánh giá
        document.getElementById('rating-count').textContent = `(${ratingCount} đánh giá)`;
    }

    function updateRatingStars() {
        const avgRatingText = document.getElementById('average-rating').textContent;
        const avgRating = parseFloat(avgRatingText);
        const starsHtml = generateStars(avgRating);
        document.getElementById('rating-stars-display').innerHTML = starsHtml;
    }


    function addComment() {
        if (!isLoggedIn) {
            toastr.warning("Cần phải đăng nhập để đăng đánh giá!");
            return;
        }

        const ratingInput = document.querySelector('input[name="rating"]:checked');
        if (!ratingInput) {
            toastr.warning("Vui lòng chọn đánh giá của bạn!");
            return;
        }
        const rating = parseInt(ratingInput.value);

        const commentTextElem = document.getElementById('comment-text');
        const commentText = commentTextElem.value.trim();
        if (!commentText) {
            toastr.warning("Vui lòng nhập nội dung đánh giá!");
            return;
        }

        if (typeof menuItemId === 'undefined') {
            toastr.error("Không xác định được MenuItemID.");
            return;
        }

        const data = {
            menuItemId: menuItemId,
            rating: rating,
            comment: commentText
        };

        fetch('/Home/AddComment', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data)
        })
        .then(response => {
            if (!response.ok) {
                throw new Error("Network response was not ok");
            }
            return response.json();
        })
        .then(result => {
            if (!result.success) {
                toastr.error(result.message);
                return;
            }
            toastr.success("Bình luận của bạn đã được thêm thành công!");
            allComments.unshift(result);
            currentPage = 1;
            displayComments();
            updatePagination();
            updateCommentCount();
            updateRatingDisplay();
            updateRatingStars();

            commentTextElem.value = "";
            document.querySelectorAll('input[name="rating"]').forEach(radio => {
                radio.checked = false;
            });
            selectedRating = 0;
        })
        .catch(error => {
            console.error("Error adding comment:", error);
            toastr.error("Có lỗi xảy ra khi thêm bình luận. Vui lòng thử lại sau.");
        });
    }

    window.addComment = addComment;

    prevPageBtn.addEventListener('click', () => { 
        if (currentPage > 1) { 
            currentPage--; 
            displayComments(); 
            updatePagination(); 
        }
    });
    nextPageBtn.addEventListener('click', () => { 
        if (currentPage < Math.ceil(allComments.length / commentsPerPage)) { 
            currentPage++; 
            displayComments(); 
            updatePagination(); 
        }
    });
    
    const submitBtn = document.getElementById('submit-comment');
    if (submitBtn) {
        submitBtn.addEventListener('click', addComment);
    }

    displayComments();
    updatePagination();
    updateCommentCount();
});
