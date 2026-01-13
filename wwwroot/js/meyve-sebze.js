let products = [];
let productTypes = {};

document.addEventListener('DOMContentLoaded', async () => {
    const productSelect = document.getElementById('productSelect');
    const productTypeGroup = document.getElementById('productTypeGroup');
    const productTypeSelect = document.getElementById('productTypeSelect');
    const priceInput = document.getElementById('priceInput');
    const checkButton = document.getElementById('checkButton');

    // Ürün listesini yükle
    try {
        products = await api.getProduceList();
        
        // Benzersiz ürün adlarını al
        const uniqueProducts = [...new Set(products.map(p => p.productName))].sort();
        
        productSelect.innerHTML = '<option value="">Ürün seçin...</option>' +
            uniqueProducts.map(product => 
                `<option value="${product}">${product}</option>`
            ).join('');

        // Her ürün için türleri grupla
        products.forEach(product => {
            const productName = product.productName;
            const productType = product.productType;
            
            if (!productTypes[productName]) {
                productTypes[productName] = [];
            }
            if (productType && !productTypes[productName].includes(productType)) {
                productTypes[productName].push(productType);
            }
        });
    } catch (error) {
        console.error('Ürünler yüklenirken hata:', error);
        productSelect.innerHTML = '<option value="">Ürünler yüklenemedi</option>';
    }

    // Ürün seçildiğinde türleri göster
    productSelect.addEventListener('change', (e) => {
        const selectedProduct = e.target.value;
        
        if (selectedProduct && productTypes[selectedProduct] && productTypes[selectedProduct].length > 1) {
            productTypeGroup.style.display = 'block';
            productTypeSelect.innerHTML = '<option value="">Tür seçin (opsiyonel)</option>' +
                productTypes[selectedProduct].map(type => 
                    `<option value="${type}">${type}</option>`
                ).join('');
        } else {
            productTypeGroup.style.display = 'none';
            productTypeSelect.innerHTML = '<option value="">Tür seçin...</option>';
        }

        updateCheckButton();
    });

    // Fiyat değiştiğinde butonu güncelle
    priceInput.addEventListener('input', updateCheckButton);

    function updateCheckButton() {
        const hasProduct = productSelect.value;
        const hasPrice = priceInput.value && parseFloat(priceInput.value) > 0;
        checkButton.disabled = !(hasProduct && hasPrice);
    }

    // Karşılaştırma butonu
    checkButton.addEventListener('click', async () => {
        const product = productSelect.value;
        const type = productTypeSelect.value || null;
        const price = parseFloat(priceInput.value);

        if (!product || !price || price <= 0) {
            alert('Lütfen ürün ve geçerli bir fiyat girin.');
            return;
        }

        checkButton.disabled = true;
        checkButton.textContent = 'Kontrol ediliyor...';

        try {
            const result = await api.comparePrice(product, price, type);
            displayResult(result);
        } catch (error) {
            console.error('Fiyat karşılaştırılırken hata:', error);
            alert('Fiyat karşılaştırılırken bir hata oluştu. Lütfen tekrar deneyin.');
        } finally {
            checkButton.disabled = false;
            checkButton.textContent = 'Normal mi?';
        }
    });
});

function displayResult(result) {
    const resultCard = document.getElementById('resultCard');
    const resultTitle = document.getElementById('resultTitle');
    const resultBadge = document.getElementById('resultBadge');
    const userPrice = document.getElementById('userPrice');
    const marketPrice = document.getElementById('marketPrice');
    const difference = document.getElementById('difference');
    const differencePercentage = document.getElementById('differencePercentage');

    resultTitle.textContent = result.produceInfo?.productName || 'Sonuç';
    resultBadge.textContent = result.result;
    resultBadge.className = `result-badge ${result.result.toLowerCase()}`;

    userPrice.textContent = `${result.userPrice.toFixed(2)} TL`;
    
    if (result.marketPrice > 0) {
        marketPrice.textContent = `${result.marketPrice.toFixed(2)} TL`;
        
        const diffText = result.difference >= 0 
            ? `+${result.difference.toFixed(2)} TL`
            : `${result.difference.toFixed(2)} TL`;
        difference.textContent = diffText;
        difference.style.color = result.difference >= 0 ? 'var(--danger-color)' : 'var(--success-color)';

        const percentageText = result.differencePercentage >= 0
            ? `+${result.differencePercentage.toFixed(1)}%`
            : `${result.differencePercentage.toFixed(1)}%`;
        differencePercentage.textContent = percentageText;
        differencePercentage.style.color = result.differencePercentage >= 0 ? 'var(--danger-color)' : 'var(--success-color)';
    } else {
        marketPrice.textContent = 'Bulunamadı';
        difference.textContent = '-';
        differencePercentage.textContent = '-';
    }

    resultCard.style.display = 'block';
    resultCard.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

