let selectedGame = null;
let searchTimeout = null;

document.addEventListener('DOMContentLoaded', () => {
    const gameSearch = document.getElementById('gameSearch');
    const gameDropdown = document.getElementById('gameDropdown');
    const selectedGameGroup = document.getElementById('selectedGameGroup');
    const selectedGameCard = document.getElementById('selectedGameCard');
    const priceInput = document.getElementById('priceInput');
    const checkButton = document.getElementById('checkButton');
    const resultCard = document.getElementById('resultCard');

    // Oyun arama - debounce ile
    gameSearch.addEventListener('input', (e) => {
        const query = e.target.value.trim();
        
        clearTimeout(searchTimeout);
        
        if (query.length < 2) {
            gameDropdown.style.display = 'none';
            return;
        }

        searchTimeout = setTimeout(async () => {
            gameDropdown.innerHTML = '<div class="game-dropdown-item">Aranıyor...</div>';
            gameDropdown.style.display = 'block';

            try {
                const games = await api.searchGames(query);
                displayGameSearchResults(games, query);
            } catch (error) {
                console.error('Oyun arama hatası:', error);
                gameDropdown.innerHTML = '<div class="game-dropdown-item">Arama başarısız. Sayfayı yenileyin.</div>';
                gameDropdown.style.display = 'block';
            }
        }, 400);
    });

    // Oyun seçildiğinde
    gameDropdown.addEventListener('click', (e) => {
        const gameItem = e.target.closest('.game-dropdown-item');
        if (gameItem) {
            const gameId = gameItem.dataset.gameId;
            const gameTitle = gameItem.dataset.gameTitle;
            const gameThumb = gameItem.dataset.gameThumb || '';
            
            selectedGame = {
                gameId: gameId,
                title: gameTitle,
                thumb: gameThumb
            };

            displaySelectedGame(selectedGame);
            gameDropdown.style.display = 'none';
            gameSearch.value = gameTitle;
            updateCheckButton();
        }
    });

    // Fiyat değiştiğinde butonu güncelle
    priceInput.addEventListener('input', updateCheckButton);

    function updateCheckButton() {
        const hasGame = selectedGame !== null;
        const hasPrice = priceInput.value && parseFloat(priceInput.value) > 0;
        checkButton.disabled = !(hasGame && hasPrice);
    }

    // Karşılaştırma butonu
    checkButton.addEventListener('click', async () => {
        if (!selectedGame || !priceInput.value) {
            alert('Lütfen oyun ve geçerli bir fiyat girin.');
            return;
        }

        const price = parseFloat(priceInput.value);
        if (price <= 0) {
            alert('Fiyat 0\'dan büyük olmalıdır.');
            return;
        }

        checkButton.disabled = true;
        checkButton.textContent = 'Kontrol ediliyor...';

        try {
            const comparison = await api.compareGamePrice(selectedGame.gameId, price);
            displayResult(comparison);
        } catch (error) {
            console.error('Fiyat karşılaştırma hatası:', error);
            alert('Fiyat karşılaştırılırken bir hata oluştu.');
        } finally {
            checkButton.disabled = false;
            checkButton.textContent = 'Normal mi?';
        }
    });

    function displayGameSearchResults(games, query = '') {
        if (!games || games.length === 0) {
            gameDropdown.innerHTML = `<div class="game-dropdown-item">"${query}" için sonuç bulunamadı</div>`;
            gameDropdown.style.display = 'block';
            return;
        }

        gameDropdown.innerHTML = games.map(game => `
            <div class="game-dropdown-item" data-game-id="${game.gameId}" data-game-title="${game.title}" data-game-thumb="${game.thumb || ''}">
                ${game.thumb ? `<img src="${game.thumb}" alt="${game.title}" class="game-thumb-small">` : ''}
                <span>${game.title}</span>
            </div>
        `).join('');

        gameDropdown.style.display = 'block';
    }

    function displaySelectedGame(game) {
        selectedGameGroup.style.display = 'block';
        selectedGameCard.innerHTML = `
            ${game.thumb ? `<img src="${game.thumb}" alt="${game.title}" class="selected-game-thumb">` : ''}
            <div class="selected-game-info">
                <h3>${game.title}</h3>
                <button class="btn-remove-game" onclick="clearSelectedGame()">✕ Kaldır</button>
            </div>
        `;
    }

    // Global function for remove button
    window.clearSelectedGame = function() {
        selectedGame = null;
        selectedGameGroup.style.display = 'none';
        gameSearch.value = '';
        priceInput.value = '';
        resultCard.style.display = 'none';
        updateCheckButton();
    };

    function displayResult(comparison) {
        resultCard.style.display = 'block';
        
        const resultTitle = document.getElementById('resultTitle');
        const resultBadge = document.getElementById('resultBadge');
        const resultEmoji = document.getElementById('resultEmoji');
        const resultMessage = document.getElementById('resultMessage');
        const userPrice = document.getElementById('userPrice');
        const averagePrice = document.getElementById('averagePrice');
        const lowestPrice = document.getElementById('lowestPrice');
        const difference = document.getElementById('difference');
        const differencePercentage = document.getElementById('differencePercentage');
        const cheapestDealCard = document.getElementById('cheapestDealCard');
        const dealStoreName = document.getElementById('dealStoreName');
        const dealPrice = document.getElementById('dealPrice');
        const dealBuyLink = document.getElementById('dealBuyLink');
        const priceBarContainer = document.getElementById('priceBarContainer');
        const priceBarMarker = document.getElementById('priceBarMarker');
        const priceBarFill = document.getElementById('priceBarFill');
        const newSearchButton = document.getElementById('newSearchButton');

        const resultType = comparison.result.toLowerCase();
        resultTitle.textContent = comparison.gameInfo?.title || selectedGame?.title || 'Sonuç';
        resultBadge.textContent = comparison.result;
        resultBadge.className = `result-badge ${resultType}`;
        
        // Kart sınıfını güncelle
        resultCard.className = `result-card ${resultType}`;

        // Emoji ve mesaj ayarla
        let emoji = '';
        let message = '';
        
        if (resultType === 'ucuz') {
            emoji = '🥳';
            const percentage = Math.abs(comparison.differencePercentage).toFixed(1);
            message = `Piyasaya göre %${percentage} daha ucuza buldunuz! Harika bir fırsat.`;
        } else if (resultType === 'normal') {
            emoji = '😊';
            message = 'Fiyatınız piyasa ortalamasına yakın. Normal bir fiyat.';
        } else if (resultType === 'pahali' || resultType === 'pahalı') {
            emoji = '😵‍💫';
            const percentage = comparison.differencePercentage.toFixed(1);
            message = `Piyasaya göre %${percentage} daha pahalı. Daha uygun fiyatlı alternatifler arayabilirsiniz.`;
        } else {
            emoji = '❓';
            message = 'Fiyat karşılaştırması yapılamadı.';
        }
        
        resultEmoji.textContent = emoji;
        resultMessage.textContent = message;

        // Format prices in USD
        userPrice.textContent = `$${comparison.userPrice.toFixed(2)}`;
        
        if (comparison.averagePrice > 0) {
            averagePrice.textContent = `$${comparison.averagePrice.toFixed(2)}`;
            lowestPrice.textContent = `$${comparison.lowestPrice.toFixed(2)}`;
            
            const diffText = comparison.difference >= 0 
                ? `+$${comparison.difference.toFixed(2)}`
                : `$${comparison.difference.toFixed(2)}`;
            difference.textContent = diffText;
            difference.style.color = comparison.difference >= 0 ? 'var(--danger-color)' : 'var(--success-color)';

            const percentageText = comparison.differencePercentage >= 0
                ? `+${comparison.differencePercentage.toFixed(1)}%`
                : `${comparison.differencePercentage.toFixed(1)}%`;
            differencePercentage.textContent = percentageText;
            differencePercentage.style.color = comparison.differencePercentage >= 0 ? 'var(--danger-color)' : 'var(--success-color)';
            
            // Fiyat baremi göster
            priceBarContainer.style.display = 'block';
            
            // Basit bir fiyat baremi için (min, avg, max varsayılan değerler)
            const minPrice = comparison.lowestPrice;
            const maxPrice = comparison.averagePrice * 1.3; // Ortalamanın %130'u
            const range = maxPrice - minPrice;
            const userPosition = range > 0 ? ((comparison.userPrice - minPrice) / range) * 100 : 50;
            const avgPosition = range > 0 ? ((comparison.averagePrice - minPrice) / range) * 100 : 50;
            
            // Marker pozisyonu
            priceBarMarker.style.left = `${Math.max(0, Math.min(100, userPosition))}%`;
            priceBarFill.style.width = `${Math.max(0, Math.min(100, avgPosition))}%`;
        } else {
            averagePrice.textContent = 'Bilinmiyor';
            lowestPrice.textContent = 'Bilinmiyor';
            difference.textContent = '-';
            differencePercentage.textContent = '-';
            priceBarContainer.style.display = 'none';
        }

        // En ucuz deal kartı - USD formatında
        if (comparison.cheapestDeal) {
            cheapestDealCard.style.display = 'block';
            dealStoreName.textContent = comparison.cheapestDeal.storeName;
            dealPrice.textContent = `$${comparison.cheapestDeal.price.toFixed(2)}`;
            dealBuyLink.href = comparison.cheapestDeal.buyUrl;
        } else {
            cheapestDealCard.style.display = 'none';
        }

        // Yeni arama butonu - mevcut listener'ları kaldır ve yeni ekle
        newSearchButton.replaceWith(newSearchButton.cloneNode(true));
        const newSearchBtn = document.getElementById('newSearchButton');
        newSearchBtn.addEventListener('click', () => {
            // Form'u temizle
            document.getElementById('gameSearch').value = '';
            document.getElementById('priceInput').value = '';
            document.getElementById('selectedGameGroup').style.display = 'none';
            selectedGame = null;
            resultCard.style.display = 'none';
            
            // Oyun aramaya odaklan
            document.getElementById('gameSearch').focus();
        });

        // Scroll to result
        resultCard.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }
});

