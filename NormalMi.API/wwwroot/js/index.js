let allCategories = [];

document.addEventListener('DOMContentLoaded', async () => {
    const categoriesGrid = document.getElementById('categoriesGrid');
    const categorySearch = document.getElementById('categorySearch');
    const searchButton = document.getElementById('searchButton');
    
    try {
        allCategories = await api.getCategories();
        
        if (allCategories.length === 0) {
            categoriesGrid.innerHTML = '<p>Henüz kategori bulunmamaktadır.</p>';
            return;
        }

        displayCategories(allCategories);

        // Arama fonksiyonu
        function filterCategories() {
            const query = categorySearch.value.toLowerCase().trim();
            
            if (query === '') {
                displayCategories(allCategories);
                return;
            }

            const filtered = allCategories.filter(category => 
                category.name.toLowerCase().includes(query) ||
                category.description.toLowerCase().includes(query)
            );

            displayCategories(filtered);
        }

        // Arama input'u için event listener
        categorySearch.addEventListener('input', filterCategories);
        categorySearch.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                filterCategories();
            }
        });

        // Arama butonu için event listener
        searchButton.addEventListener('click', filterCategories);

    } catch (error) {
        console.error('Kategoriler yüklenirken hata:', error);
        categoriesGrid.innerHTML = '<p class="error">Kategoriler yüklenirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.</p>';
    }
});

function displayCategories(categories) {
    const categoriesGrid = document.getElementById('categoriesGrid');
    
    if (categories.length === 0) {
        categoriesGrid.innerHTML = '<p class="no-results">Aradığınız kriterlere uygun kategori bulunamadı.</p>';
        return;
    }

    const categoriesHTML = categories.map(category => {
        // Route'u düzelt - eğer .html yoksa ekle
        let route = category.route;
        if (!route.endsWith('.html') && !route.startsWith('http')) {
            route = route + '.html';
        }
        
        return `
        <a href="${route}" class="category-card ${!category.isActive ? 'disabled' : ''}">
            <img src="${category.imageUrl}" alt="${category.name}" class="category-card-image" onerror="this.src='data:image/svg+xml,%3Csvg xmlns=\'http://www.w3.org/2000/svg\' width=\'400\' height=\'300\'%3E%3Crect fill=\'%23e5e7eb\' width=\'400\' height=\'300\'/%3E%3Ctext x=\'50%25\' y=\'50%25\' text-anchor=\'middle\' dy=\'.3em\' fill=\'%236b7280\' font-family=\'sans-serif\' font-size=\'18\'%3E${category.name}%3C/text%3E%3C/svg%3E'">
            <div class="category-card-content">
                <h2>${category.name}</h2>
            </div>
        </a>
    `;
    }).join('');
    
    // "Daha fazla kategori gelecek" kartını ekle
    const comingSoonCard = `
        <div class="category-card coming-soon-card">
            <div class="coming-soon-icon">➕</div>
            <div class="category-card-content">
                <h2>Daha Fazla Kategori</h2>
                <p>Yakında daha fazla kategori eklenecek!</p>
            </div>
        </div>
    `;
    
    categoriesGrid.innerHTML = categoriesHTML + comingSoonCard;
}
