document.addEventListener('DOMContentLoaded', function () {
    console.log('Shopping cart script loaded');

    // Add to cart functionality
    const addToCartButtons = document.querySelectorAll('.add-to-cart-btn');
    console.log('Found add to cart buttons:', addToCartButtons.length);

    addToCartButtons.forEach(button => {
        button.addEventListener('click', async function (e) {
            e.preventDefault();
            console.log('Add to cart button clicked');

            const productId = this.dataset.productId;
            console.log('Product ID:', productId);

            if (!productId) {
                console.error('Product ID is missing');
                showToast('error', 'Không tìm thấy thông tin sản phẩm');
                return;
            }

            try {
                // Lấy antiforgery token từ form
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                if (!token) {
                    console.error('Antiforgery token not found');
                    showToast('error', 'Có lỗi xảy ra khi thêm vào giỏ hàng');
                    return;
                }

                // Tạo URLSearchParams để gửi dữ liệu
                const params = new URLSearchParams();
                params.append('productId', productId);
                params.append('quantity', '1');

                const response = await fetch('/ShoppingCart/AddToCart', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'RequestVerificationToken': token,
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: params.toString()
                });

                console.log('Response status:', response.status);

                // Kiểm tra nếu response không phải là JSON
                const contentType = response.headers.get('content-type');
                if (!contentType || !contentType.includes('application/json')) {
                    // Nếu response là HTML (có thể là trang đăng nhập), chuyển hướng đến trang đăng nhập
                    if (response.status === 401) {
                        window.location.href = '/Identity/Account/Login';
                        return;
                    }
                    throw new Error('Server returned non-JSON response');
                }

                const result = await response.json();
                console.log('Response data:', result);

                if (result.success) {
                    showToast('success', 'Đã thêm sản phẩm vào giỏ hàng');
                    updateCartCount();
                } else {
                    showToast('error', result.message || 'Có lỗi xảy ra khi thêm vào giỏ hàng');
                }
            } catch (error) {
                console.error('Error adding to cart:', error);
                showToast('error', 'Có lỗi xảy ra khi thêm vào giỏ hàng');
            }
        });
    });

    const cartpage = document.querySelector('.cart-page');
    if (cartpage) {
        const checkboxes = document.querySelectorAll(".product-checkbox");
        const selectAllCheckbox = document.getElementById("selectAll");
        const checkoutButton = document.querySelector(".checkout-button");
        const cartTotalElements = document.querySelectorAll(".cart-total");

        // Cập nhật tổng tiền
        function updateTotal() {
            let total = 0;
            checkboxes.forEach(checkbox => {
                if (checkbox.checked) {
                    const row = checkbox.closest("tr");
                    const price = parseInt(row.querySelector(".item-price").dataset.price);
                    const quantity = parseInt(row.querySelector(".product-quantity").value);
                    total += price * quantity;
                }
            });

            // Hiển thị tổng tiền trong bảng tổng đơn hàng
            cartTotalElements.forEach(el => el.textContent = total.toLocaleString("vi-VN") + "đ");

            // Bật/tắt nút thanh toán
            checkoutButton.disabled = total === 0;
        }

        // Chọn tất cả sản phẩm
        selectAllCheckbox.addEventListener("change", function () {
            checkboxes.forEach(checkbox => {
                checkbox.checked = this.checked;
            });
            updateTotal();
        });

        // Chọn từng sản phẩm -> Cập nhật tổng đơn hàng
        checkboxes.forEach(checkbox => {
            checkbox.addEventListener("change", updateTotal);
        });

        // Xử lý tăng/giảm số lượng
        document.addEventListener("click", function (event) {
            if (event.target.classList.contains("quantity-btn")) {
                event.preventDefault();

                const input = event.target.parentNode.querySelector(".product-quantity");
                let quantity = parseInt(input.value);
                const max = parseInt(input.getAttribute("max"));
                const min = parseInt(input.getAttribute("min"));
                const productId = input.getAttribute("data-product-id");

                if (event.target.classList.contains("increase") && quantity < max) {
                    quantity++;
                } else if (event.target.classList.contains("decrease") && quantity > min) {
                    quantity--;
                }
                updateTotal();
                input.value = quantity;
                input.dispatchEvent(new Event("change"));

                // 🔥 Gửi AJAX cập nhật số lượng sản phẩm trong Session
                fetch("/ShoppingCart/UpdateCart", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({ productId: productId, quantity: quantity }),
                })
                    .then(response => response.json())
                    .then(data => {
                        console.log("Cập nhật thành công:", data);
                        // Nếu cần, cập nhật lại tổng tiền trên giao diện
                        document.querySelector(".total-price").innerText = data.totalPrice + "đ";
                    })
                    .catch(error => console.error("Lỗi cập nhật giỏ hàng:", error));
            }
        });


        // Xử lý sự kiện change duy nhất cho input số lượng
        document.querySelectorAll(".product-quantity").forEach(input => {
            input.addEventListener("change", function () {
                let quantity = parseInt(this.value);
                const max = parseInt(this.getAttribute("max"));
                const min = parseInt(this.getAttribute("min"));

                if (isNaN(quantity) || quantity < min) {
                    this.value = min;
                } else if (quantity > max) {
                    this.value = max;
                }

                // Cập nhật tổng giá từng sản phẩm
                const row = this.closest("tr");
                const price = parseInt(row.querySelector(".item-price").dataset.price);
                row.querySelector(".item-total").textContent = (price * this.value).toLocaleString("vi-VN") + "đ";

                // Nếu sản phẩm được chọn -> cập nhật tổng đơn hàng
                updateTotal();
            });
        });

        // Sự kiện checkout
    }
    
});
function showToast(type, message) {
    console.log('Showing toast:', type, message);
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = message;
    document.body.appendChild(toast);

    // Add animation class
    setTimeout(() => toast.classList.add('show'), 10);

    // Remove toast after 3 seconds
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 2000);
}

function updateCartCount() {
    const cartCountElement = document.querySelector('.cart-count');
    if (cartCountElement) {
        const currentCount = parseInt(cartCountElement.textContent) || 0;
        cartCountElement.textContent = currentCount + 1;
    }
}