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

    static async addCategory(name, description = '') {
        const response = await fetch(CAT_API_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name: name, description: description })
        });
        if (!response.ok) {
            const error = await response.json().catch(() => ({ message: 'Erro desconhecido' }));
            throw new Error(error.message || 'Falha ao cadastrar categoria');
        }
        return response.json();
    }

    static async updateCategory(id, name, description = '') {
        const response = await fetch(`${CAT_API_URL}/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name: name, description: description })
        });
        if (!response.ok) {
            const error = await response.json().catch(() => ({ message: 'Erro desconhecido' }));
            throw new Error(error.message || 'Falha ao atualizar categoria');
        }
        return true;
    }

    static async deleteCategory(id) {
        const response = await fetch(`${CAT_API_URL}/${id}`, { method: 'DELETE' });
        if (!response.ok) throw new Error('Falha ao remover categoria');
        return true;
    }

    static async getDashboardStats() {
        const response = await fetch('/api/dashboard/stats');
        if (!response.ok) throw new Error('Falha ao obter estatísticas do dashboard');
        return response.json();
    }

    static async getDashboardLogs() {
        const response = await fetch('/api/dashboard/logs');
        if (!response.ok) throw new Error('Falha ao obter logs do dashboard');
        return response.json();
    }
}

// UI Manager
class UIManager {
    constructor() {
        this.products = [];
        this.categories = [];
        this.allProducts = [];
        this.pieChart = null;
        this.barChart = null;
        this.currentPage = 1;
        this.pageSize = 10;
        this.sortField = 'name';
        this.sortDirection = 'asc';
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
        this.categoryFilterSelect = document.getElementById('category-filter');
        
        // Elementos de Paginação
        this.paginationControls = document.getElementById('pagination-controls');
        this.paginationInfo = document.getElementById('pagination-info');
        this.btnPrevPage = document.getElementById('btn-prev-page');
        this.btnNextPage = document.getElementById('btn-next-page');

        // Container de Logs
        this.logsContainer = document.getElementById('logs-container');

        // Confirm Modal
        this.confirmModal = document.getElementById('confirm-modal');
    }

    bindEvents() {
        // Search (Debounce)
        let debounceTimer;
        this.searchInput.addEventListener('input', (e) => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => this.loadProducts(e.target.value), 300);
        });

        // Eventos de Paginação
        this.btnPrevPage.addEventListener('click', () => {
            if (this.currentPage > 1) {
                this.currentPage--;
                this.renderTable();
            }
        });
        this.btnNextPage.addEventListener('click', () => {
            const maxPage = Math.ceil(this.products.length / this.pageSize);
            if (this.currentPage < maxPage) {
                this.currentPage++;
                this.renderTable();
            }
        });

        // Evento de Atualização de Logs
        document.getElementById('btn-refresh-logs').addEventListener('click', () => this.loadLogs());

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

        // Exports
        document.getElementById('btn-export-csv').addEventListener('click', () => {
            window.open('/api/products/export/csv', '_blank');
        });
        document.getElementById('btn-export-pdf').addEventListener('click', () => {
            window.open('/api/products/export/pdf', '_blank');
        });
        document.getElementById('btn-export-xml').addEventListener('click', () => {
            window.open('/api/products/export/xml', '_blank');
        });

        // Category Filter
        this.categoryFilterSelect.addEventListener('change', () => {
            this.loadProducts(this.searchInput.value);
        });

        // Category Add / Update
        document.getElementById('btn-save-category').addEventListener('click', async () => {
            const idInput = document.getElementById('edit-category-id');
            const nameInput = document.getElementById('new-category-name');
            const descInput = document.getElementById('new-category-desc');
            const id = idInput.value;
            const name = nameInput.value.trim();
            const desc = descInput.value.trim();
            
            if (!name) return;

            try {
                if (id) {
                    await ApiService.updateCategory(id, name, desc);
                    this.showToast('Categoria atualizada!', 'success');
                } else {
                    await ApiService.addCategory(name, desc);
                    this.showToast('Categoria adicionada!', 'success');
                }
                
                idInput.value = '';
                nameInput.value = '';
                descInput.value = '';
                document.getElementById('btn-save-category').textContent = 'Salvar';
                document.getElementById('btn-cancel-category-edit').classList.add('hidden');
                
                await this.loadCategories();
                this.renderCategoriesList();
                this.loadProducts(this.searchInput.value); // refresh products to show new category name if updated
                this.loadLogs();
            } catch (err) {
                this.showToast(err.message, 'error');
            }
        });
        
        document.getElementById('btn-cancel-category-edit').addEventListener('click', () => {
            document.getElementById('edit-category-id').value = '';
            document.getElementById('new-category-name').value = '';
            document.getElementById('new-category-desc').value = '';
            document.getElementById('btn-save-category').textContent = 'Salvar';
            document.getElementById('btn-cancel-category-edit').classList.add('hidden');
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
                this.loadLogs();
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
                this.loadLogs();
            } catch (error) {
                this.showToast(error.message, 'error');
            }
        });
    }

    async loadInitialData() {
        await this.loadCategories();
        await this.loadProducts();
        await this.loadLogs();
    }

    async loadLogs() {
        try {
            const logs = await ApiService.getDashboardLogs();
            this.renderLogs(logs);
        } catch (e) {
            console.error('Erro ao carregar logs:', e);
            this.logsContainer.innerHTML = '<div style="color: var(--danger);">Falha ao carregar logs de auditoria.</div>';
        }
    }

    renderLogs(logs) {
        this.logsContainer.innerHTML = '';
        if (logs.length === 0) {
            this.logsContainer.innerHTML = '<div style="color: var(--text-muted);">Nenhuma atividade registrada ainda.</div>';
            return;
        }
        
        logs.forEach(log => {
            const div = document.createElement('div');
            div.style.padding = '4px 0';
            div.style.borderBottom = '1px solid rgba(255, 255, 255, 0.05)';
            
            let formattedLog = log;
            if (log.includes('PRODUTO ADICIONADO')) {
                formattedLog = log.replace('PRODUTO ADICIONADO', '<span style="color: var(--success); font-weight: bold;">PRODUTO ADICIONADO</span>');
            } else if (log.includes('ESTOQUE ATUALIZADO')) {
                formattedLog = log.replace('ESTOQUE ATUALIZADO', '<span style="color: var(--warning); font-weight: bold;">ESTOQUE ATUALIZADO</span>');
            } else if (log.includes('PRODUTO ATUALIZADO')) {
                formattedLog = log.replace('PRODUTO ATUALIZADO', '<span style="color: var(--accent); font-weight: bold;">PRODUTO ATUALIZADO</span>');
            } else if (log.includes('PRODUTO REMOVIDO')) {
                formattedLog = log.replace('PRODUTO REMOVIDO', '<span style="color: var(--danger); font-weight: bold;">PRODUTO REMOVIDO</span>');
            } else if (log.includes('CATEGORIA ADICIONADA')) {
                formattedLog = log.replace('CATEGORIA ADICIONADA', '<span style="color: var(--success); font-weight: bold;">CATEGORIA ADICIONADA</span>');
            } else if (log.includes('CATEGORIA ATUALIZADA')) {
                formattedLog = log.replace('CATEGORIA ATUALIZADA', '<span style="color: var(--accent); font-weight: bold;">CATEGORIA ATUALIZADA</span>');
            } else if (log.includes('CATEGORIA REMOVIDA')) {
                formattedLog = log.replace('CATEGORIA REMOVIDA', '<span style="color: var(--danger); font-weight: bold;">CATEGORIA REMOVIDA</span>');
            }
            
            div.innerHTML = formattedLog;
            this.logsContainer.appendChild(div);
        });
        
        this.logsContainer.scrollTop = this.logsContainer.scrollHeight;
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
        this.categoryFilterSelect.innerHTML = '<option value="">Todas as Categorias</option>';
        
        this.categories.forEach(c => {
            const opt1 = document.createElement('option');
            opt1.value = c.id;
            opt1.textContent = c.name;
            this.productCategorySelect.appendChild(opt1);
            
            const opt2 = document.createElement('option');
            opt2.value = c.id;
            opt2.textContent = c.name;
            this.categoryFilterSelect.appendChild(opt2);
        });
    }

    editCategory(id) {
        const cat = this.categories.find(c => c.id === id);
        if (cat) {
            document.getElementById('edit-category-id').value = cat.id;
            document.getElementById('new-category-name').value = cat.name;
            document.getElementById('new-category-desc').value = cat.description || '';
            document.getElementById('btn-save-category').textContent = 'Atualizar';
            document.getElementById('btn-cancel-category-edit').classList.remove('hidden');
        }
    }

    renderCategoriesList() {
        this.categoriesTbody.innerHTML = '';
        this.categories.forEach(c => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${c.name}</td>
                <td><span style="font-size: 0.85rem; color: var(--text-muted)">${c.description || '-'}</span></td>
                <td style="text-align: center; display: flex; gap: 4px; justify-content: center;">
                    <button class="btn-icon edit" onclick="app.editCategory('${c.id}')" title="Editar">✏️</button>
                    <button class="btn-icon delete" onclick="app.deleteCategory('${c.id}')" title="Remover">🗑️</button>
                </td>
            `;
            this.categoriesTbody.appendChild(tr);
        });
    }

    async deleteCategory(id) {
        const confirmed = await this.showConfirmModal(
            '🗑️ Remover Categoria',
            'Remover esta categoria? Produtos associados ficarão <strong>sem categoria</strong>. Esta ação não pode ser desfeita.'
        );
        if (confirmed) {
            try {
                await ApiService.deleteCategory(id);
                this.showToast('Categoria removida.', 'success');
                await this.loadCategories();
                this.renderCategoriesList();
                this.loadProducts(this.searchInput.value);
                this.loadLogs();
            } catch (e) {
                this.showToast(e.message, 'error');
            }
        }
    }

    async loadProducts(searchQuery = '') {
        this.showLoading(true);
        try {
            this.allProducts = await ApiService.getProducts(searchQuery);
            this.applyFilter();
        } catch (error) {
            this.showToast('Erro ao carregar produtos', 'error');
            console.error(error);
        } finally {
            this.showLoading(false);
        }
    }

    applyFilter() {
        const catFilter = this.categoryFilterSelect.value;
        if (catFilter) {
            this.products = this.allProducts.filter(p => p.categoryId === catFilter);
        } else {
            this.products = [...this.allProducts];
        }
        this.sortProducts();
        this.currentPage = 1;
        this.renderTable();
        this.updateDashboard();
    }

    async updateDashboard() {
        try {
            const stats = await ApiService.getDashboardStats();
            this.animateCounter(this.kpiTotalItems, stats.totalItems, 700);
            this.animateCounter(this.kpiTotalValue, stats.totalValue, 700, formatCurrency);
            this.animateCounter(this.kpiLowStock, stats.lowStockCount, 700);
        } catch (e) {
            console.error('Erro ao buscar estatísticas do dashboard:', e);
            // Fallback em memória
            const totalItems = this.products.length;
            const totalValue = this.products.reduce((acc, p) => acc + (p.price * p.quantity), 0);
            const lowStock = this.products.filter(p => p.quantity < 10).length;
            this.animateCounter(this.kpiTotalItems, totalItems, 700);
            this.animateCounter(this.kpiTotalValue, totalValue, 700, formatCurrency);
            this.animateCounter(this.kpiLowStock, lowStock, 700);
        }
        
        this.renderCharts();
    }

    renderCharts() {
        if (!window.Chart) return; // Prevent errors if Chart.js fails to load
        
        const catMap = {};
        this.categories.forEach(c => {
            catMap[c.id] = { name: c.name, count: 0, value: 0 };
        });
        catMap['uncategorized'] = { name: 'Sem Categoria', count: 0, value: 0 };

        // We chart based on allProducts (ignoring search text filter usually, or we can chart the filtered ones. Let's use filtered products.)
        this.products.forEach(p => {
            const catId = p.categoryId || 'uncategorized';
            if (!catMap[catId]) catMap[catId] = { name: 'Desconhecido', count: 0, value: 0 };
            catMap[catId].count += 1;
            catMap[catId].value += (p.price * p.quantity);
        });

        // Filter out empty categories
        const activeCats = Object.values(catMap).filter(c => c.count > 0);
        
        const labels = activeCats.map(c => c.name);
        const countData = activeCats.map(c => c.count);
        const valueData = activeCats.map(c => c.value);

        const colors = ['#00bfa5', '#42a5f5', '#ab47bc', '#ffca28', '#ef5350', '#66bb6a', '#ffa726', '#8d6e63', '#78909c'];

        Chart.defaults.color = '#e0e0e0';
        Chart.defaults.borderColor = 'rgba(255, 255, 255, 0.1)';

        // Pie Chart
        const pieCtx = document.getElementById('category-pie-chart').getContext('2d');
        if (this.pieChart) this.pieChart.destroy();
        this.pieChart = new Chart(pieCtx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: countData,
                    backgroundColor: colors,
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom' },
                    title: { display: true, text: 'Produtos por Categoria', color: '#fff', font: { size: 16 } }
                }
            }
        });

        // Bar Chart
        const barCtx = document.getElementById('category-bar-chart').getContext('2d');
        if (this.barChart) this.barChart.destroy();
        this.barChart = new Chart(barCtx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Valor em Estoque (R$)',
                    data: valueData,
                    backgroundColor: 'rgba(0, 191, 165, 0.6)',
                    borderColor: '#00bfa5',
                    borderWidth: 1,
                    borderRadius: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    title: { display: true, text: 'Valor em Estoque por Categoria', color: '#fff', font: { size: 16 } }
                },
                scales: {
                    y: { beginAtZero: true }
                }
            }
        });
    }

    renderTable() {
        this.tableBody.innerHTML = '';
        
        if (this.products.length === 0) {
            this.emptyState.classList.remove('hidden');
            this.paginationControls.classList.add('hidden');
        } else {
            this.emptyState.classList.add('hidden');
            
            // Lógica de Paginação
            const totalItems = this.products.length;
            const maxPage = Math.ceil(totalItems / this.pageSize) || 1;
            if (this.currentPage > maxPage) this.currentPage = maxPage;
            
            const startIdx = (this.currentPage - 1) * this.pageSize;
            const endIdx = Math.min(startIdx + this.pageSize, totalItems);
            const pagedProducts = this.products.slice(startIdx, endIdx);
            
            // Habilitar/desabilitar botões
            this.btnPrevPage.disabled = this.currentPage === 1;
            this.btnNextPage.disabled = this.currentPage === maxPage;
            
            this.paginationInfo.textContent = `Exibindo ${totalItems === 0 ? 0 : startIdx + 1}-${endIdx} de ${totalItems} produtos`;
            this.paginationControls.classList.remove('hidden');
            
            pagedProducts.forEach(product => {
                const tr = document.createElement('tr');
                
                // Badge de estoque
                let stockBadge = '';
                if (product.quantity === 0) stockBadge = '<span class="badge badge-out">Sem Estoque</span>';
                else if (product.quantity < 10) stockBadge = '<span class="badge badge-low">Baixo</span>';

                // Barra de saúde do estoque
                const stockPercent = Math.min(100, (product.quantity / 100) * 100);
                const stockColor = product.quantity === 0
                    ? 'var(--danger)'
                    : product.quantity < 10
                        ? 'var(--warning)'
                        : 'var(--success)';
                
                const catName = product.categoryId ? (this.categories.find(c => c.id === product.categoryId)?.name || '-') : '-';
                const dataDisplay = product.updatedAt ? `<div style="font-size:0.8rem;color:var(--text-muted)">Atualizado:<br>${formatDate(product.updatedAt)}</div>` : `<div style="font-size:0.8rem;color:var(--text-muted)">Criado:<br>${formatDate(product.createdAt)}</div>`;

                tr.innerHTML = `
                    <td style="font-size: 0.8rem; color: var(--text-muted)">${product.id.split('-')[0]}</td>
                    <td class="highlight-text">${product.name}</td>
                    <td>${catName}</td>
                    <td>${formatCurrency(product.price)}</td>
                    <td>
                        <div>${product.quantity} ${stockBadge}</div>
                        <div class="stock-bar-container"><div class="stock-bar" style="width: ${stockPercent}%; background: ${stockColor};"></div></div>
                    </td>
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
        if (!product) return;
        const confirmed = await this.showConfirmModal(
            '🗑️ Remover Produto',
            `Deseja realmente remover o produto <strong>${product.name}</strong>?<br><span style="font-size:0.85rem">Esta ação não pode ser desfeita.</span>`
        );
        if (confirmed) {
            try {
                await ApiService.deleteProduct(id);
                this.showToast('Produto removido com sucesso!', 'success');
                this.loadProducts(this.searchInput.value);
                this.loadLogs();
            } catch (error) {
                this.showToast(error.message, 'error');
            }
        }
    }

    // --- Ordenação por Colunas ---
    toggleSort(field) {
        if (this.sortField === field) {
            this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
        } else {
            this.sortField = field;
            this.sortDirection = 'asc';
        }
        this.updateSortIndicators();
        this.sortProducts();
        this.currentPage = 1;
        this.renderTable();
    }

    updateSortIndicators() {
        document.querySelectorAll('th.sortable').forEach(th => {
            const field = th.dataset.sort;
            const indicator = th.querySelector('.sort-indicator');
            if (field === this.sortField) {
                th.classList.add('active');
                indicator.textContent = this.sortDirection === 'asc' ? '▲' : '▼';
            } else {
                th.classList.remove('active');
                indicator.textContent = '▲';
            }
        });
    }

    sortProducts() {
        const field = this.sortField;
        const dir = this.sortDirection === 'asc' ? 1 : -1;
        this.products.sort((a, b) => {
            const aVal = a[field];
            const bVal = b[field];
            if (typeof aVal === 'string') {
                return aVal.localeCompare(bVal, 'pt-BR') * dir;
            }
            return (aVal - bVal) * dir;
        });
    }

    // --- Animação de Contagem KPI ---
    animateCounter(element, target, duration = 700, formatter = null) {
        const startTime = performance.now();
        element.classList.remove('animate');
        void element.offsetWidth; // force reflow para reiniciar animação CSS
        element.classList.add('animate');
        const tick = (now) => {
            const elapsed = now - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 3); // ease-out cubic
            const current = target * eased;
            element.textContent = formatter ? formatter(current) : Math.round(current);
            if (progress < 1) requestAnimationFrame(tick);
        };
        requestAnimationFrame(tick);
    }

    // --- Modal de Confirmação Customizado ---
    showConfirmModal(title, message) {
        return new Promise((resolve) => {
            document.getElementById('confirm-modal-title').textContent = title;
            document.getElementById('confirm-modal-message').innerHTML = message;
            this.confirmModal.classList.remove('hidden');

            const btnOk = document.getElementById('btn-ok-confirm');
            const btnCancel = document.getElementById('btn-cancel-confirm');
            const btnClose = document.getElementById('btn-close-confirm-modal');

            const cleanup = (result) => {
                this.confirmModal.classList.add('hidden');
                btnOk.removeEventListener('click', onOk);
                btnCancel.removeEventListener('click', onCancel);
                btnClose.removeEventListener('click', onCancel);
                resolve(result);
            };

            const onOk = () => cleanup(true);
            const onCancel = () => cleanup(false);

            btnOk.addEventListener('click', onOk);
            btnCancel.addEventListener('click', onCancel);
            btnClose.addEventListener('click', onCancel);
        });
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
