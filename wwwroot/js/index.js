document.addEventListener('DOMContentLoaded', async () => {
    const categoriesGrid = document.getElementById('categoriesGrid');
    
    try {
        const categories = await api.getCategories();
        
        if (categories.length === 0) {
            categoriesGrid.innerHTML = '<p>Henüz kategori bulunmamaktadır.</p>';
            return;
        }

        categoriesGrid.innerHTML = categories.map(category => `
            <a href="${category.route}" class="category-card ${!category.isActive ? 'disabled' : ''}">
                <h2>${category.name}</h2>
                <p>${category.description}</p>
            </a>
        `).join('');
    } catch (error) {
        console.error('Kategoriler yüklenirken hata:', error);
        categoriesGrid.innerHTML = '<p class="error">Kategoriler yüklenirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.</p>';
    }
});

