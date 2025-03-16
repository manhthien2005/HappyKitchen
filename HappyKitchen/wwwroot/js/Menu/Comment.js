document.addEventListener('DOMContentLoaded', function () {
    let currentPage = 1;
    const commentsPerPage = 5;
    const commentList = document.getElementById('comment-list');
    const prevPageBtn = document.getElementById('prev-page');
    const nextPageBtn = document.getElementById('next-page');
    const totalComments = document.getElementById('comment-count');
    const currentPageDisplay = document.getElementById('current-page');
    const totalPagesDisplay = document.getElementById('total-pages');
    let allComments = [];

    function updatePagination() {
        const totalPages = Math.ceil(allComments.length / commentsPerPage);
        currentPageDisplay.textContent = currentPage;
        totalPagesDisplay.textContent = totalPages;
        prevPageBtn.disabled = currentPage === 1;
        nextPageBtn.disabled = currentPage >= totalPages || totalPages === 0;
    }

    function generateStars(rating) {
        return Array.from({ length: 5 }, (_, i) =>
            `<span class="star ${i < rating ? 'full' : 'empty'}">★</span>`
        ).join('');
    }

    function displayComments() {
        commentList.innerHTML = '';
        if (allComments.length === 0) {
            commentList.innerHTML = '<div class="comment">No comments yet. Be the first to comment!</div>';
            return;
        }

        allComments.slice((currentPage - 1) * commentsPerPage, currentPage * commentsPerPage)
            .forEach(comment => {
                const commentElement = document.createElement('div');
                commentElement.className = 'comment';
                commentElement.innerHTML = `
                    <div class="comment-header">
                        <span class="comment-author">${comment.author}</span>
                        <span class="comment-date">${comment.date}</span>
                    </div>
                    <div style="margin-bottom: 10px;">${generateStars(comment.rating)}</div>
                    <div class="comment-text">${comment.text}</div>
                `;
                commentList.appendChild(commentElement);
            });
    }

    function updateCommentCount() {
        totalComments.textContent = `${allComments.length} comments`;
    }

    function addComment() {
        const name = document.getElementById('comment-name').value.trim();
        const commentText = document.getElementById('comment-text').value.trim();
        const ratingInput = document.querySelector('input[name="rating"]:checked');

        if (!name || !commentText || !ratingInput) {
            alert('Please fill in all fields and select a rating.');
            return;
        }

        allComments.unshift({
            author: name,
            date: new Date().toISOString().split('T')[0],
            text: commentText,
            rating: parseInt(ratingInput.value)
        });

        currentPage = 1;
        displayComments();
        updatePagination();
        updateCommentCount();

        document.getElementById('comment-form').reset();
    }

    prevPageBtn.addEventListener('click', () => { if (currentPage > 1) { currentPage--; displayComments(); updatePagination(); } });
    nextPageBtn.addEventListener('click', () => { if (currentPage < Math.ceil(allComments.length / commentsPerPage)) { currentPage++; displayComments(); updatePagination(); } });
    document.getElementById('submit-comment').addEventListener('click', addComment);

    displayComments();
    updatePagination();
    updateCommentCount();
});
