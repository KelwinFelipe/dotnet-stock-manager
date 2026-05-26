const API_BASE_URL = '/api/products';
const CAT_API_URL = '/api/categories';

// Formatação de Moeda BRL
const formatCurrency = (value) => {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
};

const formatDate = (dateString) => {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute:'2-digit' });
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

    static async updateProduct(id, product) {
        const response = await fetch(`${API_BASE_URL}/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(product)
        });
        
        if (!response.ok) {
            const error = await response.json().catch(() => ({ message: 'Erro desconhecido' }));
            throw new Error(error.message || 'Falha ao atualizar produto');
        }
        return true;
    }

    static async updateQuantity(id, quantity) {
        const response = await fetch(`${API_BASE_URL}/${id}/quantity`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(quantity)
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

    // --- Categorias ---
    static async getCategories() {
        const response = await fetch(CAT_API_URL);
        if (!response.ok) throw new Error('Falha ao buscar categorias');
        return response.json();
    }

    static async addCategory(name) {
        const response = await fetch(CAT_API_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name: name, description: '' })
        });
        if (!response.ok) {
            const error = await response.json().catch(() => ({ message: 'Erro desconhecido' }));
            throw new Error(error.message || 'Falha ao cadastrar categoria');
        }
        return response.json();
    }

    static async deleteCategory(id) {
        const response = await fetch(`${CAT_API_URL}/${id}`, { method: 'DELETE' });
        if (!response.ok) throw new Error('Falha ao remover categoria');
        return true;
    }
}

// UI Manager
class UIManager {
    constructor() {
        this.products = [];
        this.categories = [];
        this.initElements();
        this.bindEvents();
        this.loadInitialData();
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
        this.categoryModal = document.getElementById('category-modal');
        
        // Forms
        this.productForm = document.getElementById('product-form');
        this.quantityForm = document.getElementById('quantity-form');
        this.productCategorySelect = document.getElementById('product-category');
        this.categoriesTbody = document.getElementById('categories-tbody');
    }

    bindEvents() {
        // Search (Debounce)
        let debounceTimer;
        this.searchInput.addEventListener('input', (e) => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => this.loadProducts(e.target.value), 300);
        });

        // Modals Opening
        document.getElementById('btn-new-product').addEventListener('click', () => {
            document.getElementById('modal-title').textContent = 'Cadastrar Produto';
            document.getElementById('edit-product-id').value = '';
            this.productForm.reset();
            this.productModal.classList.remove('hidden');
        });

        document.getElementById('btn-manage-categories').addEventListener('click', () => {
            this.categoryModal.classList.remove('hidden');
            this.renderCategoriesList();
        });

        // Close Modals
        document.getElementById('btn-close-modal').addEventListener('click', () => this.productModal.classList.add('hidden'));
        document.getElementById('btn-cancel-modal').addEventListener('click', () => this.productModal.classList.add('hidden'));
        
        document.getElementById('btn-close-quantity-modal').addEventListener('click', () => this.quantityModal.classList.add('hidden'));
        document.getElementById('btn-cancel-quantity-modal').addEventListener('click', () => this.quantityModal.classList.add('hidden'));

        document.getElementById('btn-close-category-modal').addEventListener('click', () => this.categoryModal.classList.add('hidden'));
        document.getElementById('btn-done-category-modal').addEventListener('click', () => this.categoryModal.classList.add('hidden'));

        // Category Add
        document.getElementById('btn-add-category').addEventListener('click', async () => {
            const input = document.getElementById('new-category-name');
            const name = input.value.trim();
            if (!name) return;

            try {
                await ApiService.addCategory(name);
                input.value = '';
                this.showToast('Categoria adicionada!', 'success');
                await this.loadCategories();
                this.renderCategoriesList();
            } catch (err) {
                this.showToast(err.message, 'error');
            }
        });

        // Forms Submit
        this.productForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const id = document.getElementById('edit-product-id').value;
            const categoryId = this.productCategorySelect.value;
            
            const product = {
                name: document.getElementById('product-name').value,
                price: parseFloat(document.getElementById('product-price').value),
                quantity: parseInt(document.getElementById('product-quantity').value, 10),
                categoryId: categoryId || null
            };
            
            try {
                if (id) {
                    await ApiService.updateProduct(id, product);
                    this.showToast('Produto atualizado com sucesso!', 'success');
                } else {
                    await ApiService.addProduct(product);
                    this.showToast('Produto cadastrado com sucesso!', 'success');
                }
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

    async loadInitialData() {
        await this.loadCategories();
        await this.loadProducts();
    }

    async loadCategories() {
        try {
            this.categories = await ApiService.getCategories();
            this.updateCategorySelect();
        } catch (e) {
            console.error(e);
        }
    }

    updateCategorySelect() {
        this.productCategorySelect.innerHTML = '<option value="">Sem Categoria</option>';
        this.categories.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = c.name;
            this.productCategorySelect.appendChild(opt);
        });
    }

    renderCategoriesList() {
        this.categoriesTbody.innerHTML = '';
        this.categories.forEach(c => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${c.name}</td>
                <td style="text-align: center;">
                    <button class="btn-icon delete" onclick="app.deleteCategory('${c.id}')" title="Remover">🗑️</button>
                </td>
            `;
            this.categoriesTbody.appendChild(tr);
        });
    }

    async deleteCategory(id) {
        if(confirm('Remover esta categoria? Produtos associados ficarão sem categoria.')) {
            try {
                await ApiService.deleteCategory(id);
                this.showToast('Categoria removida.', 'success');
                await this.loadCategories();
                this.renderCategoriesList();
                this.loadProducts(this.searchInput.value); // refresh table
            } catch (e) {
                this.showToast(e.message, 'error');
            }
        }
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
                
                const catName = product.categoryId ? (this.categories.find(c => c.id === product.categoryId)?.name || '-') : '-';
                const dataDisplay = product.updatedAt ? `<div style="font-size:0.8rem;color:var(--text-muted)">Atualizado:<br>${formatDate(product.updatedAt)}</div>` : `<div style="font-size:0.8rem;color:var(--text-muted)">Criado:<br>${formatDate(product.createdAt)}</div>`;

                tr.innerHTML = `
                    <td style="font-size: 0.8rem; color: var(--text-muted)">${product.id.split('-')[0]}</td>
                    <td class="highlight-text">${product.name}</td>
                    <td>${catName}</td>
                    <td>${formatCurrency(product.price)}</td>
                    <td>${product.quantity} ${stockBadge}</td>
                    <td>${dataDisplay}</td>
                    <td class="action-cell">
                        <button class="btn-icon edit" onclick="app.openEditProductModal('${product.id}')" title="Editar Produto">✏️</button>
                        <button class="btn-icon edit" onclick="app.openQuantityModal('${product.id}')" title="Atualizar Estoque rápido">📦</button>
                        <button class="btn-icon delete" onclick="app.deleteProduct('${product.id}')" title="Remover Produto">🗑️</button>
                    </td>
                `;
                this.tableBody.appendChild(tr);
            });
        }
    }

    openEditProductModal(id) {
        const product = this.products.find(p => p.id === id);
        if (product) {
            document.getElementById('modal-title').textContent = 'Editar Produto';
            document.getElementById('edit-product-id').value = product.id;
            document.getElementById('product-name').value = product.name;
            document.getElementById('product-price').value = product.price;
            document.getElementById('product-quantity').value = product.quantity;
            this.productCategorySelect.value = product.categoryId || '';
            this.productModal.classList.remove('hidden');
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
