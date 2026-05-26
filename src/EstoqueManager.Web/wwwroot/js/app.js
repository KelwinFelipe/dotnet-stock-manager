const API_BASE_URL = '/api/products';

// Formatação de Moeda BRL
const formatCurrency = (value) => {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
};

// API Service
class ApiService {
    static async getProducts(searchQuery = '') {
        const url = searchQuery ? `${API_BASE_URL}/search?q=${encodeURIComponent(searchQuery)}` : API_BASE_URL;
        const response = await fetch(url);
        if (!response.ok) throw new Error('Falha ao buscar produtos');
        return response.json();
    }

    static async addProduct(product) {
        const response = await fetch(API_BASE_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(product)
        });
        
        if (!response.ok) {
            const error = await response.json().catch(() => ({ message: 'Erro desconhecido' }));
            throw new Error(error.message || 'Falha ao cadastrar produto');
        }
        return response.json();
    }

    static async updateQuantity(id, quantity) {
        const response = await fetch(`${API_BASE_URL}/${id}/quantity`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(quantity) // Minimal API espera o valor direto no body com [FromBody] se for um int. Caso não funcione, será ajustado.
        });
        
        if (!response.ok) throw new Error('Falha ao atualizar quantidade');
        return true;
    }

    static async deleteProduct(id) {
        const response = await fetch(`${API_BASE_URL}/${id}`, {
            method: 'DELETE'
        });
        
        if (!response.ok) throw new Error('Falha ao remover produto');
        return true;
    }
}

// UI Manager
class UIManager {
    constructor() {
        this.products = [];
        this.initElements();
        this.bindEvents();
        this.loadProducts();
    }

    initElements() {
        this.tableBody = document.getElementById('products-tbody');
        this.spinner = document.getElementById('loading-spinner');
        this.emptyState = document.getElementById('empty-state');
        
        // KPIs
        this.kpiTotalItems = document.getElementById('kpi-total-items');
        this.kpiTotalValue = document.getElementById('kpi-total-value');
        this.kpiLowStock = document.getElementById('kpi-low-stock');
        
        // Search
        this.searchInput = document.getElementById('search-input');
        
        // Modals
        this.productModal = document.getElementById('product-modal');
        this.quantityModal = document.getElementById('quantity-modal');
        
        // Forms
        this.productForm = document.getElementById('product-form');
        this.quantityForm = document.getElementById('quantity-form');
    }

    bindEvents() {
        // Search (Debounce)
        let debounceTimer;
        this.searchInput.addEventListener('input', (e) => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => this.loadProducts(e.target.value), 300);
        });

        // New Product
        document.getElementById('btn-new-product').addEventListener('click', () => {
            this.productForm.reset();
            this.productModal.classList.remove('hidden');
        });

        // Close Modals
        document.getElementById('btn-close-modal').addEventListener('click', () => this.productModal.classList.add('hidden'));
        document.getElementById('btn-cancel-modal').addEventListener('click', () => this.productModal.classList.add('hidden'));
        
        document.getElementById('btn-close-quantity-modal').addEventListener('click', () => this.quantityModal.classList.add('hidden'));
        document.getElementById('btn-cancel-quantity-modal').addEventListener('click', () => this.quantityModal.classList.add('hidden'));

        // Forms Submit
        this.productForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const product = {
                name: document.getElementById('product-name').value,
                price: parseFloat(document.getElementById('product-price').value),
                quantity: parseInt(document.getElementById('product-quantity').value, 10)
            };
            
            try {
                await ApiService.addProduct(product);
                this.showToast('Produto cadastrado com sucesso!', 'success');
                this.productModal.classList.add('hidden');
                this.loadProducts(this.searchInput.value);
            } catch (error) {
                this.showToast(error.message, 'error');
            }
        });

        this.quantityForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const id = document.getElementById('edit-quantity-id').value;
            const quantity = parseInt(document.getElementById('new-quantity').value, 10);
            
            try {
                await ApiService.updateQuantity(id, quantity);
                this.showToast('Quantidade atualizada com sucesso!', 'success');
                this.quantityModal.classList.add('hidden');
                this.loadProducts(this.searchInput.value);
            } catch (error) {
                this.showToast(error.message, 'error');
            }
        });
    }

    async loadProducts(searchQuery = '') {
        this.showLoading(true);
        try {
            this.products = await ApiService.getProducts(searchQuery);
            this.renderTable();
            this.updateDashboard();
        } catch (error) {
            this.showToast('Erro ao carregar produtos', 'error');
            console.error(error);
        } finally {
            this.showLoading(false);
        }
    }

    updateDashboard() {
        const totalItems = this.products.length;
        const totalValue = this.products.reduce((acc, p) => acc + (p.price * p.quantity), 0);
        const lowStock = this.products.filter(p => p.quantity < 10).length;

        this.kpiTotalItems.textContent = totalItems;
        this.kpiTotalValue.textContent = formatCurrency(totalValue);
        this.kpiLowStock.textContent = lowStock;
    }

    renderTable() {
        this.tableBody.innerHTML = '';
        
        if (this.products.length === 0) {
            this.emptyState.classList.remove('hidden');
        } else {
            this.emptyState.classList.add('hidden');
            
            this.products.forEach(product => {
                const tr = document.createElement('tr');
                
                // Badge de estoque
                let stockBadge = '';
                if (product.quantity === 0) stockBadge = '<span class="badge badge-out">Sem Estoque</span>';
                else if (product.quantity < 10) stockBadge = '<span class="badge badge-low">Baixo</span>';
                
                tr.innerHTML = `
                    <td style="font-size: 0.8rem; color: var(--text-muted)">${product.id.split('-')[0]}...</td>
                    <td class="highlight-text">${product.name}</td>
                    <td>${formatCurrency(product.price)}</td>
                    <td>${product.quantity} ${stockBadge}</td>
                    <td class="action-cell">
                        <button class="btn-icon edit" onclick="app.openQuantityModal('${product.id}')" title="Atualizar Estoque">📦</button>
                        <button class="btn-icon delete" onclick="app.deleteProduct('${product.id}')" title="Remover Produto">🗑️</button>
                    </td>
                `;
                this.tableBody.appendChild(tr);
            });
        }
    }

    openQuantityModal(id) {
        const product = this.products.find(p => p.id === id);
        if (product) {
            document.getElementById('edit-quantity-id').value = product.id;
            document.getElementById('edit-quantity-name').textContent = product.name;
            document.getElementById('new-quantity').value = product.quantity;
            this.quantityModal.classList.remove('hidden');
        }
    }

    async deleteProduct(id) {
        const product = this.products.find(p => p.id === id);
        if (product && confirm(`Deseja realmente remover o produto '${product.name}'?`)) {
            try {
                await ApiService.deleteProduct(id);
                this.showToast('Produto removido com sucesso!', 'success');
                this.loadProducts(this.searchInput.value);
            } catch (error) {
                this.showToast(error.message, 'error');
            }
        }
    }

    showLoading(show) {
        if (show) {
            this.spinner.classList.remove('hidden');
            this.tableBody.innerHTML = '';
            this.emptyState.classList.add('hidden');
        } else {
            this.spinner.classList.add('hidden');
        }
    }

    showToast(message, type = 'success') {
        const container = document.getElementById('toast-container');
        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        toast.textContent = message;
        
        container.appendChild(toast);
        
        // Trigger reflow to animate
        setTimeout(() => toast.classList.add('show'), 10);
        
        // Auto remove
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }
}

// Inicializar a aplicação
const app = new UIManager();
