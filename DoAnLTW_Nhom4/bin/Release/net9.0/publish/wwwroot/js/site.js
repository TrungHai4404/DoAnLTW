document.addEventListener("DOMContentLoaded", function () {
    const hamburger = document.getElementById('hamburger');
    const navLinks = document.querySelector('.nav-links');
    const overlay = document.getElementById('menu-overlay');

    // Khi nhấn vào hamburger
    hamburger.addEventListener('click', function () {
        navLinks.classList.toggle('active');  // Thêm hoặc loại bỏ class 'active' để hiển thị menu
        overlay.classList.toggle('active');  // Hiển thị nền tối
        hamburger.style.display = 'none';  // Ẩn hamburger khi menu hiển thị
    });

    // Khi nhấn vào overlay (nền tối bên ngoài menu)
    overlay.addEventListener('click', function () {
        navLinks.classList.remove('active');  // Đóng menu
        overlay.classList.remove('active');  // Ẩn overlay
        hamburger.style.display = 'block';  // Hiển thị lại hamburger
    });

    $("#search-title").hide(); // Ẩn tiêu đề nếu không có sản phẩm
    $("#product-container").hide(); // Ẩn danh sách sản phẩm
    function loadProducts() {
        let search = $("#searchQuery").val() || "";
        let categoryId = $(".category-list a.active").data("id") || null;
        let brandId = $(".brand-list a.active").data("id") || null;
        let minPrice = $("#minPrice").val() ? parseFloat($("#minPrice").val()) : null;
        let maxPrice = $("#maxPrice").val() ? parseFloat($("#maxPrice").val()) : null;
        let sortOrder = $("#sortOrder").val() || "";
        let inStock = $("#inStock").prop("checked");
        let hasDiscount = $("#hasDiscount").prop("checked");

        console.log("Dữ liệu gửi đi:", { search, categoryId, brandId, minPrice, maxPrice, sortOrder, inStock, hasDiscount });

        $.ajax({
            url: "/Product/Filter",
            type: "GET",
            data: { search, categoryId, brandId, minPrice, maxPrice, sortOrder, inStock, hasDiscount },
            success: function (result) {
                console.log("✅ Kết quả AJAX:", result);
                if (result.trim().includes("no-products")) {
                    $("#search-title").hide(); // Ẩn tiêu đề nếu không có sản phẩm
                    $("#product-container").hide(); // Ẩn danh sách sản phẩm
                    $("#no-products-container").html(result).show(); // Hiển thị thông báo
                } else {
                    $("#search-title").show(); // Hiển thị tiêu đề
                    $("#no-products-container").hide(); // Ẩn thông báo nếu có sản phẩm
                    $("#product-container").html(result).show(); // Hiển thị danh sách sản phẩm
                }
                
            },
            error: function (xhr, status, error) {
                console.error("❌ Lỗi AJAX:", error);
            }
        });
    }

    // Sự kiện lọc
    $("#searchQuery, #minPrice, #maxPrice, #sortOrder, #inStock, #hasDiscount").on("change keyup", function () {
        loadProducts();
    });

    // Click danh mục hoặc thương hiệu
    $(".category-list a, .brand-list a").on("click", function (e) {
        e.preventDefault();
        $(".category-list a, .brand-list a").removeClass("active");
        $(this).addClass("active");

        let categoryId = $(this).data("id");  // Lấy ID đúng cách
        console.log("📌 Chọn danh mục:", categoryId);

        loadProducts();
    });

    // Áp dụng bộ lọc khi nhấn nút
    $("#applyFilters").on("click", function () {
        loadProducts();
    });
    $("#clearFilters").on("click", function () {
        // Reset all filter inputs
        $("#minPrice").val('');
        $("#maxPrice").val('');
        $("#sortOrder").val('');
        $("#inStock").prop('checked', false);
        $("#hasDiscount").prop('checked', false);

        // Reset category and brand active states (optional)
        $(".category-item").removeClass('active');
        $(".brand-list a").removeClass('active');
        loadProducts(); // Load products with cleared filters
    });

    // Add to cart functionality
    document.querySelectorAll('.add-to-cart-btn').forEach(button => {
        button.addEventListener('click', async function (e) {
            e.preventDefault();
            const productId = this.dataset.productId;
            try {
                const response = await fetch('/ShoppingCart/AddToCart', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        productId: parseInt(productId),
                        quantity: 1
                    })
                });

                const result = await response.json();
                if (result.success) {
                    // Show success message
                    const toast = document.createElement('div');
                    toast.className = 'toast success';
                    toast.textContent = result.message;
                    document.body.appendChild(toast);
                    setTimeout(() => toast.remove(), 3000);
                } else {
                    // Show error message
                    const toast = document.createElement('div');
                    toast.className = 'toast error';
                    toast.textContent = result.message;
                    document.body.appendChild(toast);
                    setTimeout(() => toast.remove(), 3000);
                }
            } catch (error) {
                console.error('Error:', error);
                // Show error message
                const toast = document.createElement('div');
                toast.className = 'toast error';
                toast.textContent = 'Có lỗi xảy ra khi thêm vào giỏ hàng';
                document.body.appendChild(toast);
                setTimeout(() => toast.remove(), 3000);
            }
        });
    });

    // Horizontal scroll for product rows
    document.querySelectorAll('.product-row-container').forEach(container => {
        const row = container.querySelector('.product-row');
        const leftBtn = container.querySelector('.scroll-left');
        const rightBtn = container.querySelector('.scroll-right');

        // Ẩn nút scroll khi không cần thiết
        const updateScrollButtons = () => {
            leftBtn.style.display = row.scrollLeft > 0 ? 'flex' : 'none';
            rightBtn.style.display = row.scrollLeft < (row.scrollWidth - row.clientWidth) ? 'flex' : 'none';
        };

        row.addEventListener('scroll', updateScrollButtons);
        window.addEventListener('resize', updateScrollButtons);
        updateScrollButtons(); // Kiểm tra trạng thái ban đầu

        leftBtn.addEventListener('click', () => {
            row.scrollBy({
                left: -300,
                behavior: 'smooth'
            });
        });

        rightBtn.addEventListener('click', () => {
            row.scrollBy({
                left: 300,
                behavior: 'smooth'
            });
        });
    });
});