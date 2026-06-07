const API_BASE_URL = '/api';

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

