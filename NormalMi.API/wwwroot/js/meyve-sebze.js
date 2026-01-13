let products = [];
let productTypes = {};
let allUniqueProducts = [];
let filteredProducts = [];
let filteredTypes = [];
let selectedProductIndex = -1;
let selectedTypeIndex = -1;

document.addEventListener('DOMContentLoaded', async () => {
    const productSelect = document.getElementById('productSelect');
    const productDropdown = document.getElementById('productDropdown');
    const productTypeGroup = document.getElementById('productTypeGroup');
    const productTypeSelect = document.getElementById('productTypeSelect');
    const productTypeDropdown = document.getElementById('productTypeDropdown');
    const priceInput = document.getElementById('priceInput');
    const checkButton = document.getElementById('checkButton');

    // Ürün listesini yükle
    try {
        console.log('Ürün listesi yükleniyor...');
        const result = await api.getProduceList();
        
        console.log('API Response:', result);
        
        // Veriler hazırlanıyorsa loading durumunu göster
        if (result && result.loading === true) {
            console.log('Veriler hazırlanıyor...');
            productSelect.placeholder = 'Veriler hazırlanıyor, lütfen bekleyin...';
            productSelect.disabled = true;
            checkButton.disabled = true;
            
            // 5 saniye sonra tekrar dene
            setTimeout(async () => {
                try {
                    const retryResult = await api.getProduceList();
                    console.log('Retry result:', retryResult);
                    if (!retryResult.loading && Array.isArray(retryResult)) {
                        products = retryResult;
                        initializeProducts();
                    } else if (retryResult.loading) {
                        // Hala loading, 10 saniye sonra tekrar dene
                        setTimeout(async () => {
                            const finalResult = await api.getProduceList();
                            console.log('Final result:', finalResult);
                            if (!finalResult.loading && Array.isArray(finalResult)) {
                                products = finalResult;
                                initializeProducts();
                            } else {
                                productSelect.placeholder = 'Veriler yüklenemedi, sayfayı yenileyin';
                                productSelect.disabled = false;
                            }
                        }, 10000);
                    } else {
                        productSelect.placeholder = 'Veriler yüklenemedi, sayfayı yenileyin';
                        productSelect.disabled = false;
                    }
                } catch (error) {
                    console.error('Retry error:', error);
                    productSelect.placeholder = 'Veriler yüklenemedi, sayfayı yenileyin';
                    productSelect.disabled = false;
                }
            }, 5000);
            return;
        }
        
        // Normal array response (cache dolu)
        if (Array.isArray(result)) {
            products = result;
            console.log(`Toplam ${products.length} ürün yüklendi`);
            
            if (!products || products.length === 0) {
                console.warn('Ürün listesi boş!');
                productSelect.placeholder = 'Ürün bulunamadı';
                return;
            }
            
            initializeProducts();
        } else {
            console.error('Beklenmeyen response formatı:', result);
            productSelect.placeholder = 'Veriler yüklenemedi';
        }
    } catch (error) {
        console.error('Ürün listesi yüklenirken hata:', error);
        productSelect.placeholder = 'Veriler yüklenemedi';
    }
    
    function initializeProducts() {
        try {
            if (!products || products.length === 0) {
                return;
            }
            
            // Input'ları tekrar aktif et
            productSelect.disabled = false;
            productSelect.placeholder = 'Ürün seçin...';
            
            // Benzersiz ürün adlarını al
            allUniqueProducts = [...new Set(products.map(p => p.productName))].sort();
            filteredProducts = [...allUniqueProducts];
            console.log(`${allUniqueProducts.length} benzersiz ürün bulundu`);
            
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
            
            // İlk dropdown'u göster
            updateProductDropdown();
            
            console.log('Ürün listesi başarıyla yüklendi');
        } catch (error) {
            console.error('Ürünler yüklenirken hata:', error);
            productSelect.placeholder = 'Ürünler yüklenemedi';
        }
    }

    // Ürün input'unda yazıldığında
    productSelect.addEventListener('input', (e) => {
        const searchTerm = e.target.value.toLowerCase().trim();
        selectedProductIndex = -1;
        
        if (searchTerm === '') {
            filteredProducts = [...allUniqueProducts];
        } else {
            filteredProducts = allUniqueProducts.filter(product => 
                product.toLowerCase().includes(searchTerm)
            );
        }
        
        updateProductDropdown();
        productDropdown.style.display = filteredProducts.length > 0 ? 'block' : 'none';
    });

    // Ürün input'una focus olduğunda
    productSelect.addEventListener('focus', () => {
        if (filteredProducts.length > 0) {
            productDropdown.style.display = 'block';
        }
    });

    // Ürün dropdown item'ına tıklandığında
    productDropdown.addEventListener('click', (e) => {
        if (e.target.classList.contains('custom-select-dropdown-item')) {
            const selectedValue = e.target.textContent.trim();
            productSelect.value = selectedValue;
            productDropdown.style.display = 'none';
            updateProductTypes(selectedValue);
            updateCheckButton();
        }
    });

    // Dışarı tıklandığında dropdown'u kapat
    document.addEventListener('click', (e) => {
        if (!productSelect.contains(e.target) && !productDropdown.contains(e.target)) {
            productDropdown.style.display = 'none';
        }
        if (!productTypeSelect.contains(e.target) && !productTypeDropdown.contains(e.target)) {
            productTypeDropdown.style.display = 'none';
        }
    });

    // Ürün türü input'unda yazıldığında
    productTypeSelect.addEventListener('input', (e) => {
        const searchTerm = e.target.value.toLowerCase().trim();
        const selectedProduct = productSelect.value.trim();
        selectedTypeIndex = -1;
        
        const exactMatch = allUniqueProducts.find(p => 
            p.toLowerCase() === selectedProduct.toLowerCase()
        );
        
        if (!exactMatch || !productTypes[exactMatch]) {
            return;
        }
        
        const allTypes = productTypes[exactMatch];
        if (searchTerm === '') {
            filteredTypes = [...allTypes];
        } else {
            filteredTypes = allTypes.filter(type => 
                type.toLowerCase().includes(searchTerm)
            );
        }
        
        updateProductTypeDropdown();
        productTypeDropdown.style.display = filteredTypes.length > 0 ? 'block' : 'none';
    });

    // Ürün türü input'una focus olduğunda
    productTypeSelect.addEventListener('focus', () => {
        if (filteredTypes.length > 0) {
            productTypeDropdown.style.display = 'block';
        }
    });

    // Ürün türü dropdown item'ına tıklandığında
    productTypeDropdown.addEventListener('click', (e) => {
        if (e.target.classList.contains('custom-select-dropdown-item')) {
            const selectedValue = e.target.textContent.trim();
            productTypeSelect.value = selectedValue;
            productTypeDropdown.style.display = 'none';
            updateCheckButton();
        }
    });

    function updateProductDropdown() {
        productDropdown.innerHTML = filteredProducts.map((product, index) => 
            `<div class="custom-select-dropdown-item ${index === selectedProductIndex ? 'selected' : ''}" data-value="${product}">${product}</div>`
        ).join('');
    }

    function updateProductTypeDropdown() {
        productTypeDropdown.innerHTML = filteredTypes.map((type, index) => 
            `<div class="custom-select-dropdown-item ${index === selectedTypeIndex ? 'selected' : ''}" data-value="${type}">${type}</div>`
        ).join('');
    }

    function updateProductTypes(selectedProduct) {
        // Seçilen ürünün tam eşleşmesini bul
        const exactMatch = allUniqueProducts.find(p => 
            p.toLowerCase() === selectedProduct.toLowerCase()
        );
        
        if (exactMatch && productTypes[exactMatch] && productTypes[exactMatch].length > 0) {
            productTypeGroup.style.display = 'block';
            productTypeSelect.value = '';
            filteredTypes = [...productTypes[exactMatch]];
            updateProductTypeDropdown();
        } else {
            productTypeGroup.style.display = 'none';
            productTypeSelect.value = '';
            filteredTypes = [];
        }
    }

    // Fiyat değiştiğinde butonu güncelle
    priceInput.addEventListener('input', updateCheckButton);

    function updateCheckButton() {
        const hasProduct = productSelect.value && productSelect.value.trim() !== '';
        const hasPrice = priceInput.value && parseFloat(priceInput.value) > 0;
        checkButton.disabled = !(hasProduct && hasPrice);
    }

    // Karşılaştırma butonu
    checkButton.addEventListener('click', async () => {
        const product = productSelect.value.trim();
        const type = productTypeSelect.value.trim() || null;
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
    const resultEmoji = document.getElementById('resultEmoji');
    const resultMessage = document.getElementById('resultMessage');
    const resultEmojiContainer = document.getElementById('resultEmojiContainer');
    const userPrice = document.getElementById('userPrice');
    const marketPrice = document.getElementById('marketPrice');
    const difference = document.getElementById('difference');
    const differencePercentage = document.getElementById('differencePercentage');
    const priceBarContainer = document.getElementById('priceBarContainer');
    const priceBarMarker = document.getElementById('priceBarMarker');
    const priceBarFill = document.getElementById('priceBarFill');
    const newSearchButton = document.getElementById('newSearchButton');

    const resultType = result.result.toLowerCase();
    resultTitle.textContent = result.produceInfo?.productName || 'Sonuç';
    resultBadge.textContent = result.result;
    resultBadge.className = `result-badge ${resultType}`;
    
    // Kart sınıfını güncelle
    resultCard.className = `result-card ${resultType}`;

    // Emoji ve mesaj ayarla
    let emoji = '';
    let message = '';
    
    if (resultType === 'ucuz') {
        emoji = '🥳';
        const percentage = Math.abs(result.differencePercentage).toFixed(1);
        message = `Piyasaya göre %${percentage} daha ucuza buldunuz! Harika bir fırsat.`;
    } else if (resultType === 'normal') {
        emoji = '😊';
        message = 'Fiyatınız piyasa ortalamasına yakın. Normal bir fiyat.';
    } else if (resultType === 'pahali' || resultType === 'pahalı') {
        emoji = '😵‍💫';
        const percentage = result.differencePercentage.toFixed(1);
        message = `Piyasaya göre %${percentage} daha pahalı. Daha uygun fiyatlı alternatifler arayabilirsiniz.`;
    } else {
        emoji = '❓';
        message = 'Fiyat karşılaştırması yapılamadı.';
    }
    
    resultEmoji.textContent = emoji;
    resultMessage.textContent = message;

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
        
        // Fiyat baremi göster
        priceBarContainer.style.display = 'block';
        
        // Basit bir fiyat baremi için (min, avg, max varsayılan değerler)
        const minPrice = result.marketPrice * 0.7; // Ortalamanın %70'i
        const maxPrice = result.marketPrice * 1.3; // Ortalamanın %130'u
        const range = maxPrice - minPrice;
        const userPosition = ((result.userPrice - minPrice) / range) * 100;
        const avgPosition = ((result.marketPrice - minPrice) / range) * 100;
        
        // Marker pozisyonu
        priceBarMarker.style.left = `${Math.max(0, Math.min(100, userPosition))}%`;
        priceBarFill.style.width = `${Math.max(0, Math.min(100, avgPosition))}%`;
    } else {
        marketPrice.textContent = 'Bulunamadı';
        difference.textContent = '-';
        differencePercentage.textContent = '-';
        priceBarContainer.style.display = 'none';
    }

    // Yeni arama butonu - mevcut listener'ları kaldır ve yeni ekle
    newSearchButton.replaceWith(newSearchButton.cloneNode(true));
    const newSearchBtn = document.getElementById('newSearchButton');
    newSearchBtn.addEventListener('click', () => {
        // Form'u temizle
        document.getElementById('productSelect').value = '';
        document.getElementById('productTypeSelect').value = '';
        document.getElementById('priceInput').value = '';
        document.getElementById('productTypeGroup').style.display = 'none';
        resultCard.style.display = 'none';
        
        // Ürün seçimine odaklan
        document.getElementById('productSelect').focus();
    });

    resultCard.style.display = 'block';
    resultCard.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}
