// Mock Data
const categories = [
    { id: 1, name: '🎂 Cakes' },
    { id: 2, name: '🥐 Pastries' },
    { id: 3, name: '🍞 Breads' },
    { id: 4, name: '☕ Beverages' },
    { id: 5, name: '🍰 Desserts' },
    { id: 6, name: '🥪 Savory' }
];

const subcategories = {
    1: [
        { id: 11, name: 'Birthday Cakes' },
        { id: 12, name: 'Wedding Cakes' },
        { id: 13, name: 'Cupcakes' },
        { id: 14, name: 'Cheesecakes' }
    ],
    2: [
        { id: 21, name: 'Croissants' },
        { id: 22, name: 'Danish' },
        { id: 23, name: 'Tarts' },
        { id: 24, name: 'Pies' }
    ],
    3: [
        { id: 31, name: 'White Bread' },
        { id: 32, name: 'Whole Wheat' },
        { id: 33, name: 'Sourdough' },
        { id: 34, name: 'Rolls' }
    ],
    4: [
        { id: 41, name: 'Coffee' },
        { id: 42, name: 'Tea' },
        { id: 43, name: 'Smoothies' },
        { id: 44, name: 'Soft Drinks' }
    ],
    5: [
        { id: 51, name: 'Ice Cream' },
        { id: 52, name: 'Puddings' },
        { id: 53, name: 'Cookies' },
        { id: 54, name: 'Brownies' }
    ],
    6: [
        { id: 61, name: 'Sandwiches' },
        { id: 62, name: 'Quiches' },
        { id: 63, name: 'Sausage Rolls' },
        { id: 64, name: 'Pizza' }
    ]
};

const products = {
    11: [
        { id: 111, name: 'Chocolate Birthday Cake', price: 250.00 },
        { id: 112, name: 'Vanilla Birthday Cake', price: 220.00 },
        { id: 113, name: 'Red Velvet Cake', price: 280.00 },
        { id: 114, name: 'Funfetti Cake', price: 240.00 }
    ],
    12: [
        { id: 121, name: '3-Tier Wedding Cake', price: 1500.00 },
        { id: 122, name: '5-Tier Wedding Cake', price: 2500.00 },
        { id: 123, name: 'Cupcake Tower', price: 800.00 }
    ],
    13: [
        { id: 131, name: 'Chocolate Cupcakes (6)', price: 60.00 },
        { id: 132, name: 'Vanilla Cupcakes (6)', price: 55.00 },
        { id: 133, name: 'Red Velvet Cupcakes (6)', price: 65.00 }
    ],
    21: [
        { id: 211, name: 'Plain Croissant', price: 25.00 },
        { id: 212, name: 'Chocolate Croissant', price: 30.00 },
        { id: 213, name: 'Almond Croissant', price: 35.00 }
    ],
    31: [
        { id: 311, name: 'White Loaf', price: 18.00 },
        { id: 312, name: 'White Rolls (6 pack)', price: 22.00 }
    ],
    41: [
        { id: 411, name: 'Cappuccino', price: 32.00 },
        { id: 412, name: 'Latte', price: 35.00 },
        { id: 413, name: 'Espresso', price: 25.00 },
        { id: 414, name: 'Americano', price: 28.00 }
    ],
    51: [
        { id: 511, name: 'Vanilla Ice Cream', price: 45.00 },
        { id: 512, name: 'Chocolate Ice Cream', price: 45.00 },
        { id: 513, name: 'Strawberry Ice Cream', price: 45.00 }
    ],
    61: [
        { id: 611, name: 'Chicken Mayo Sandwich', price: 35.00 },
        { id: 612, name: 'Ham & Cheese Sandwich', price: 32.00 },
        { id: 613, name: 'Tuna Sandwich', price: 38.00 }
    ]
};

// State
let currentView = 'categories';
let currentCategory = null;
let currentSubcategory = null;
let selectedProduct = null;
let cart = [];
let totalAmount = 0;

// Initialize
document.addEventListener('DOMContentLoaded', function() {
    showCategories();
    setupKeyboardShortcuts();
});

// Keyboard Shortcuts
function setupKeyboardShortcuts() {
    document.addEventListener('keydown', function(e) {
        if (e.key === 'F12') {
            e.preventDefault();
            openTenderScreen();
        }
        // Add other F-key shortcuts as needed
    });
}

function showCategories() {
    currentView = 'categories';
    currentCategory = null;
    currentSubcategory = null;
    document.getElementById('breadcrumb').textContent = 'Categories';
    
    const grid = document.getElementById('gridContainer');
    grid.innerHTML = '';
    
    categories.forEach(cat => {
        const item = document.createElement('div');
        item.className = 'grid-item';
        item.textContent = cat.name;
        item.onclick = () => showSubcategories(cat);
        grid.appendChild(item);
    });
}

function showSubcategories(category) {
    currentView = 'subcategories';
    currentCategory = category;
    document.getElementById('breadcrumb').innerHTML = `<span style="cursor:pointer" onclick="showCategories()">Categories</span> > ${category.name}`;
    
    const grid = document.getElementById('gridContainer');
    grid.innerHTML = '';
    
    const subs = subcategories[category.id] || [];
    subs.forEach(sub => {
        const item = document.createElement('div');
        item.className = 'grid-item subcategory';
        item.textContent = sub.name;
        item.onclick = () => showProducts(sub);
        grid.appendChild(item);
    });
}

function showProducts(subcategory) {
    currentView = 'products';
    currentSubcategory = subcategory;
    document.getElementById('breadcrumb').innerHTML = `<span style="cursor:pointer" onclick="showCategories()">Categories</span> > <span style="cursor:pointer" onclick="showSubcategories(currentCategory)">${currentCategory.name}</span> > ${subcategory.name}`;
    
    const grid = document.getElementById('gridContainer');
    grid.innerHTML = '';
    
    const prods = products[subcategory.id] || [];
    if (prods.length === 0) {
        grid.innerHTML = '<p style="grid-column: 1/-1; text-align: center; font-size: 20px; color: #999; padding: 50px;">No products available</p>';
        return;
    }
    
    prods.forEach(prod => {
        const item = document.createElement('div');
        item.className = 'grid-item product';
        item.innerHTML = `
            ${prod.name}
            <div class="product-price">R ${prod.price.toFixed(2)}</div>
        `;
        item.onclick = () => openQuantityModal(prod);
        grid.appendChild(item);
    });
}

// Quantity Modal
function openQuantityModal(product) {
    selectedProduct = product;
    document.getElementById('modalTitle').textContent = product.name;
    document.getElementById('quantityInput').value = '1';
    document.getElementById('quantityModal').classList.add('active');
}

function closeModal() {
    document.getElementById('quantityModal').classList.remove('active');
    selectedProduct = null;
}

function addNumber(num) {
    const input = document.getElementById('quantityInput');
    if (input.value === '0' || input.value === '1') {
        input.value = num;
    } else {
        input.value += num;
    }
}

function clearQuantity() {
    document.getElementById('quantityInput').value = '1';
}

function backspace() {
    const input = document.getElementById('quantityInput');
    if (input.value.length > 1) {
        input.value = input.value.slice(0, -1);
    } else {
        input.value = '1';
    }
}

function confirmQuantity() {
    const quantity = parseInt(document.getElementById('quantityInput').value);
    if (quantity > 0 && selectedProduct) {
        addToCart(selectedProduct, quantity);
        closeModal();
    }
}

// Cart Management
function addToCart(product, quantity) {
    const existingItem = cart.find(item => item.product.id === product.id);
    if (existingItem) {
        existingItem.quantity += quantity;
    } else {
        cart.push({ product, quantity });
    }
    updateCart();
}

function updateCart() {
    const cartItems = document.getElementById('cartItems');
    const subtotalEl = document.getElementById('subtotal');
    const vatEl = document.getElementById('vat');
    const totalEl = document.getElementById('total');
    
    if (cart.length === 0) {
        cartItems.innerHTML = '<p class="empty-cart">Cart is empty</p>';
        subtotalEl.textContent = 'R 0.00';
        vatEl.textContent = 'R 0.00';
        totalEl.textContent = 'R 0.00';
        totalAmount = 0;
        return;
    }
    
    let subtotal = 0;
    cartItems.innerHTML = '';
    
    cart.forEach((item, index) => {
        const itemTotal = item.product.price * item.quantity;
        subtotal += itemTotal;
        
        const div = document.createElement('div');
        div.className = 'cart-item';
        div.innerHTML = `
            <div class="cart-item-name">${item.product.name}</div>
            <div class="cart-item-details">
                <span>${item.quantity} x R${item.product.price.toFixed(2)}</span>
                <span style="font-weight: bold; color: var(--iron-gold);">R${itemTotal.toFixed(2)}</span>
            </div>
        `;
        cartItems.appendChild(div);
    });
    
    const vat = subtotal * 0.15;
    totalAmount = subtotal + vat;
    
    subtotalEl.textContent = `R ${subtotal.toFixed(2)}`;
    vatEl.textContent = `R ${vat.toFixed(2)}`;
    totalEl.textContent = `R ${totalAmount.toFixed(2)}`;
}

// Tender Screen
function openTenderScreen() {
    if (cart.length === 0) {
        alert('Cart is empty! Add items first.');
        return;
    }
    
    const modal = document.getElementById('tenderModal');
    const content = document.getElementById('tenderContent');
    
    content.innerHTML = `
        <div class="tender-header">
            <div class="tender-title">💳 SELECT PAYMENT METHOD</div>
            <div class="tender-amount">Total: R ${totalAmount.toFixed(2)}</div>
        </div>
        <div class="tender-buttons">
            <button class="tender-btn tender-btn-cash" onclick="processCashPayment()">
                <div class="tender-icon">💵</div>
                <div>CASH</div>
            </button>
            <button class="tender-btn tender-btn-card" onclick="processCardPayment()">
                <div class="tender-icon">💳</div>
                <div>CARD</div>
            </button>
            <button class="tender-btn tender-btn-eft" onclick="processEFTPayment()">
                <div class="tender-icon">🏦</div>
                <div>EFT</div>
            </button>
            <button class="tender-btn tender-btn-manual" onclick="processManualPayment()">
                <div class="tender-icon">✍️</div>
                <div>MANUAL</div>
            </button>
        </div>
        <button class="btn-cancel-tender" onclick="closeTenderScreen()">✖ CANCEL</button>
    `;
    
    modal.classList.add('active');
}

function closeTenderScreen() {
    document.getElementById('tenderModal').classList.remove('active');
}

// Cash Payment
function processCashPayment() {
    const content = document.getElementById('tenderContent');
    content.className = 'tender-content cash-screen';
    
    content.innerHTML = `
        <div class="cash-header">
            <div class="cash-label">AMOUNT DUE:</div>
            <div class="cash-value">R ${totalAmount.toFixed(2)}</div>
            <div class="cash-label" style="margin-top: 10px;">TENDERED: <span id="tenderedAmount">R 0.00</span></div>
            <div class="cash-label" style="color: var(--iron-gold);">CHANGE: <span id="changeAmount">R 0.00</span></div>
        </div>
        <input type="text" class="cash-input" id="cashInput" value="0.00" readonly>
        <div class="cash-keypad">
            <button class="btn-number" onclick="addCashNumber('1')">1</button>
            <button class="btn-number" onclick="addCashNumber('2')">2</button>
            <button class="btn-number" onclick="addCashNumber('3')">3</button>
            <button class="btn-number" onclick="addCashNumber('4')">4</button>
            <button class="btn-number" onclick="addCashNumber('5')">5</button>
            <button class="btn-number" onclick="addCashNumber('6')">6</button>
            <button class="btn-number" onclick="addCashNumber('7')">7</button>
            <button class="btn-number" onclick="addCashNumber('8')">8</button>
            <button class="btn-number" onclick="addCashNumber('9')">9</button>
            <button class="btn-number" onclick="addCashNumber('.')">.</button>
            <button class="btn-number" onclick="addCashNumber('0')">0</button>
            <button class="btn-number" onclick="backspaceCash()">⌫</button>
        </div>
        <div class="cash-actions">
            <button class="btn-modal btn-no" onclick="openTenderScreen()">← BACK</button>
            <button class="btn-modal btn-yes" onclick="confirmCashPayment()">✓ CONFIRM</button>
        </div>
    `;
}

function addCashNumber(num) {
    const input = document.getElementById('cashInput');
    if (input.value === '0.00') {
        input.value = num === '.' ? '0.' : num;
    } else {
        if (num === '.' && input.value.includes('.')) return;
        input.value += num;
    }
    updateCashDisplay();
}

function backspaceCash() {
    const input = document.getElementById('cashInput');
    if (input.value.length > 1) {
        input.value = input.value.slice(0, -1);
    } else {
        input.value = '0.00';
    }
    updateCashDisplay();
}

function updateCashDisplay() {
    const input = document.getElementById('cashInput');
    const tendered = parseFloat(input.value) || 0;
    const change = Math.max(0, tendered - totalAmount);
    
    document.getElementById('tenderedAmount').textContent = `R ${tendered.toFixed(2)}`;
    document.getElementById('changeAmount').textContent = `R ${change.toFixed(2)}`;
}

function confirmCashPayment() {
    const input = document.getElementById('cashInput');
    const tendered = parseFloat(input.value) || 0;
    
    if (tendered < totalAmount) {
        alert('Insufficient amount!');
        return;
    }
    
    const change = tendered - totalAmount;
    showPaymentSuccess('CASH', change);
}

// Card Payment
function processCardPayment() {
    const content = document.getElementById('tenderContent');
    content.className = 'tender-content card-screen';
    
    content.innerHTML = `
        <div class="card-icon">💳</div>
        <div class="card-instruction">INSERT OR TAP CARD</div>
        <div class="tender-amount">Amount: R ${totalAmount.toFixed(2)}</div>
        <div class="card-waiting">Waiting for customer...</div>
    `;
    
    // Simulate card processing
    setTimeout(() => {
        showCardProcessing();
    }, 2000);
}

function showCardProcessing() {
    const content = document.getElementById('tenderContent');
    content.className = 'tender-content card-screen processing-screen';
    
    content.innerHTML = `
        <div class="card-icon">⏳</div>
        <div class="card-instruction">PROCESSING AUTHORIZATION</div>
        <div class="tender-amount" style="color: var(--iron-gold);">R ${totalAmount.toFixed(2)}</div>
        <div class="card-waiting">Please wait...</div>
    `;
    
    setTimeout(() => {
        showPaymentSuccess('CARD', 0);
    }, 3000);
}

// EFT Payment
function processEFTPayment() {
    const content = document.getElementById('tenderContent');
    content.className = 'tender-content';
    
    content.innerHTML = `
        <div class="tender-header">
            <div class="tender-title">🏦 EFT PAYMENT SLIP</div>
        </div>
        <div style="background: rgba(255,255,255,0.1); padding: 30px; border-radius: 15px; margin-bottom: 20px;">
            <h3 style="color: var(--iron-gold); margin-bottom: 20px;">BANK DETAILS</h3>
            <p style="margin: 10px 0;">Bank: ABSA Bank</p>
            <p style="margin: 10px 0;">Account Name: Oven Delights (Pty) Ltd</p>
            <p style="margin: 10px 0;">Account Number: 4012345678</p>
            <p style="margin: 10px 0;">Branch Code: 632005</p>
            <p style="margin: 10px 0; color: var(--iron-gold); font-size: 24px; font-weight: bold;">Amount: R ${totalAmount.toFixed(2)}</p>
            <p style="margin: 10px 0; color: var(--iron-red);">Reference: INV-${Date.now()}</p>
        </div>
        <div style="display: flex; gap: 15px;">
            <button class="btn-modal btn-no" onclick="openTenderScreen()">← BACK</button>
            <button class="btn-modal btn-yes" onclick="showPaymentSuccess('EFT', 0)">✓ CONFIRM</button>
        </div>
    `;
}

// Manual Payment
function processManualPayment() {
    const content = document.getElementById('tenderContent');
    content.className = 'tender-content';
    
    content.innerHTML = `
        <div class="tender-header">
            <div class="tender-title">✍️ MANUAL PAYMENT</div>
            <div class="tender-amount">Total: R ${totalAmount.toFixed(2)}</div>
        </div>
        <div style="background: rgba(255,255,255,0.1); padding: 30px; border-radius: 15px; margin-bottom: 20px; text-align: center;">
            <p style="font-size: 20px; margin: 20px 0;">Manual payment entry</p>
            <p style="color: #999;">Record payment details manually</p>
        </div>
        <div style="display: flex; gap: 15px;">
            <button class="btn-modal btn-no" onclick="openTenderScreen()">← BACK</button>
            <button class="btn-modal btn-yes" onclick="showPaymentSuccess('MANUAL', 0)">✓ CONFIRM</button>
        </div>
    `;
}

// Payment Success
function showPaymentSuccess(method, change) {
    const content = document.getElementById('tenderContent');
    content.className = 'tender-content card-screen success-screen';
    
    let changeText = '';
    if (change > 0) {
        changeText = `<div class="card-waiting" style="font-size: 24px; color: var(--iron-gold);">CHANGE: R ${change.toFixed(2)}</div>`;
    }
    
    content.innerHTML = `
        <div class="success-icon">✓</div>
        <div class="card-instruction">PAYMENT APPROVED</div>
        <div class="tender-amount" style="color: var(--iron-gold);">R ${totalAmount.toFixed(2)}</div>
        <div class="card-waiting">Payment Method: ${method}</div>
        ${changeText}
        <button class="btn-modal btn-yes" style="margin-top: 30px; width: 80%;" onclick="completeTransaction()">PRINT RECEIPT & NEW SALE</button>
    `;
}

function completeTransaction() {
    // Clear cart
    cart = [];
    totalAmount = 0;
    updateCart();
    
    // Close tender screen
    closeTenderScreen();
    
    // Show success message
    alert('Transaction completed! Receipt printed.');
    
    // Return to categories
    showCategories();
}
