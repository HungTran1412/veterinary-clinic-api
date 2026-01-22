(function () {
    window.addEventListener("load", function () {
        setTimeout(function () {
            // SVG Icons
            const copyIcon = '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="feather feather-copy"><rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path></svg>';
            const checkIcon = '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#28a745" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="feather feather-check"><polyline points="20 6 9 17 4 12"></polyline></svg>';

            function addCopyButtons() {
                // Tìm thẻ cha chứa toàn bộ dòng summary (Method + Path + Description + Arrow)
                var summaries = document.querySelectorAll('.opblock-summary');
                
                summaries.forEach(function (el) {
                    if (el.querySelector('.copy-btn')) return;

                    // Lấy path từ thẻ con
                    var pathEl = el.querySelector('.opblock-summary-path');
                    if (!pathEl) return;
                    var path = pathEl.getAttribute('data-path');

                    var btn = document.createElement('button');
                    btn.className = 'copy-btn';
                    btn.innerHTML = copyIcon;
                    btn.title = 'Copy Endpoint Path'; // Đổi title cho đúng ý nghĩa
                    
                    // Style cho nút
                    btn.style.marginRight = '15px'; 
                    btn.style.marginLeft = 'auto';  
                    btn.style.border = 'none';
                    btn.style.background = 'transparent';
                    btn.style.cursor = 'pointer';
                    btn.style.padding = '4px';
                    btn.style.display = 'flex';
                    btn.style.alignItems = 'center';
                    btn.style.opacity = '0.6'; 
                    
                    // Hiệu ứng hover
                    btn.onmouseover = function() { btn.style.opacity = '1'; };
                    btn.onmouseout = function() { btn.style.opacity = '0.6'; };

                    btn.onclick = function (e) {
                        e.stopPropagation(); // Ngăn mở accordion
                        
                        // Chỉ copy path, không lấy window.location.origin nữa
                        var textToCopy = path; 
                        
                        navigator.clipboard.writeText(textToCopy).then(function () {
                            var originalHtml = btn.innerHTML;
                            btn.innerHTML = checkIcon;
                            setTimeout(function () {
                                btn.innerHTML = originalHtml;
                            }, 1500);
                        });
                    };

                    el.appendChild(btn);
                });
            }

            addCopyButtons();

            var observer = new MutationObserver(function (mutations) {
                addCopyButtons();
            });

            var target = document.querySelector('#swagger-ui');
            if (target) {
                observer.observe(target, { childList: true, subtree: true });
            }

        }, 1000);
    });
})();