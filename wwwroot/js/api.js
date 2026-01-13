// Backend URL'i - Development için
const API_BASE_URL = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
    ? 'https://localhost:7160/api'  // HTTPS port
    : '/api'; // Production'da relative path kullan

class ApiClient {
    async get(url) {
        try {
            const response = await fetch(`${API_BASE_URL}${url}`);
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
}

const api = new ApiClient();

