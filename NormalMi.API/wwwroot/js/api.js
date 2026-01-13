// Backend URL'i - Development için
const API_BASE_URL = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
    ? 'https://localhost:7160/api'  // HTTPS port
    : '/api'; // Production'da relative path kullan

class ApiClient {
    async get(url) {
        try {
            const response = await fetch(`${API_BASE_URL}${url}`);
            
            // 202 Accepted: Veriler hazırlanıyor
            if (response.status === 202) {
                const data = await response.json();
                return { loading: true, message: data.message, data: data.data || [] };
            }
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return await response.json();
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    }

    async getCategories() {
        return this.get('/categories');
    }

    async getCategory(id) {
        return this.get(`/categories/${id}`);
    }

    async getProduceList() {
        return this.get('/produce/list');
    }

    async getProducePrice(product, type = null) {
        const url = type 
            ? `/produce/price?product=${encodeURIComponent(product)}&type=${encodeURIComponent(type)}`
            : `/produce/price?product=${encodeURIComponent(product)}`;
        return this.get(url);
    }

    async comparePrice(product, price, type = null) {
        const url = type
            ? `/produce/compare?product=${encodeURIComponent(product)}&price=${price}&type=${encodeURIComponent(type)}`
            : `/produce/compare?product=${encodeURIComponent(product)}&price=${price}`;
        return this.get(url);
    }

    // Game endpoints
    async searchGames(query) {
        if (!query || query.trim().length < 2) {
            return [];
        }
        const url = `/game/search?query=${encodeURIComponent(query)}`;
        return this.get(url);
    }

    async compareGamePrice(gameId, price) {
        const url = `/game/${encodeURIComponent(gameId)}/compare?userPrice=${price}`;
        return this.get(url);
    }

    async getGameDeals(gameId) {
        const url = `/game/${encodeURIComponent(gameId)}/deals`;
        return this.get(url);
    }
}

const api = new ApiClient();
