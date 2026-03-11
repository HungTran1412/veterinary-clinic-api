(function () {
    window.addEventListener("load", function () {
        setTimeout(function () {
            // Hàm tạo nút copy
            function addCopyButtons() {
                var urlElements = document.querySelectorAll('.opblock .opblock-summary-path');
                
                urlElements.forEach(function (el) {
                    // Kiểm tra xem đã có nút copy chưa để tránh trùng lặp
                    if (el.querySelector('.copy-url-btn')) return;

                    var path = el.getAttribute('data-path'); // Lấy path từ thuộc tính data
                    if(!path) path = el.innerText.trim();    // Hoặc lấy text nếu không có data-path

                    // Tạo container cho nút copy
                    var btnContainer = document.createElement('span');
                    btnContainer.className = 'copy-url-btn';
                    btnContainer.style.marginLeft = '10px';
                    btnContainer.style.cursor = 'pointer';
                    btnContainer.style.display = 'inline-flex';
                    btnContainer.style.alignItems = 'center';
                    btnContainer.title = 'Copy Endpoint URL';

                    // Icon copy (SVG)
                    btnContainer.innerHTML = `
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                            <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                        </svg>
                    `;

                    // Sự kiện click
                    btnContainer.addEventListener('click', function (e) {
                        e.stopPropagation(); // Ngăn sự kiện mở/đóng panel của Swagger
                        
                        var fullUrl = window.location.origin + path;
                        
                        navigator.clipboard.writeText(fullUrl).then(function() {
                            // Hiệu ứng feedback khi copy thành công
                            var originalHtml = btnContainer.innerHTML;
                            btnContainer.innerHTML = `
                                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#28a745" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                    <polyline points="20 6 9 17 4 12"></polyline>
                                </svg>
                            `;
                            setTimeout(function() {
                                btnContainer.innerHTML = originalHtml;
                            }, 1500);
                        });
                    });

                    // Chèn nút vào sau text endpoint
                    el.appendChild(btnContainer);
                });
            }

            // Gọi hàm lần đầu
            addCopyButtons();

            // Quan sát thay đổi DOM để thêm nút khi user mở/đóng các nhóm API hoặc tìm kiếm
            var observer = new MutationObserver(function (mutations) {
                addCopyButtons();
            });

            var targetNode = document.querySelector('#swagger-ui');
            if (targetNode) {
                observer.observe(targetNode, { childList: true, subtree: true });
            }

        }, 1000);
    });
})();