document.addEventListener("DOMContentLoaded", () => {
    const state = {
        dashboardStats: {},
        revenueData: {
            labels: [],
            data: []
        },
        topSellingFoods: [],
        timeRanges: {
            revenue: 'month',
            topFoods: 'day'
        }
    };

    // API endpoints
    const API = {
        dashboardStats: '/Dashboard/GetDashboardStats',
        revenueData: (timeRange) => `/Dashboard/GetRevenueData?timeRange=${timeRange}`,
        topSellingFoods: (timeRange) => `/Dashboard/GetTopSellingFoods?timeRange=${timeRange}`
    };

    // DOM elements
    const DOM = {
        revenueTimeRange: document.getElementById("revenueTimeRange"),
        topFoodTimeRange: document.getElementById("topFoodTimeRange"),
        revenueChart: document.getElementById("revenueChart"),
        topFoodsList: document.querySelector(".top-foods-list"),
        statCards: {
            revenue: {
                title: document.querySelector('.stat-card.revenue .stat-card-title'),
                change: document.querySelector('.stat-card.revenue .stat-card-change')
            },
            orders: {
                title: document.querySelector('.stat-card.orders .stat-card-title'),
                change: document.querySelector('.stat-card.orders .stat-card-change')
            },
            customers: {
                title: document.querySelector('.stat-card.customers .stat-card-title'),
                change: document.querySelector('.stat-card.customers .stat-card-change')
            },
            tables: {
                title: document.querySelector('.stat-card.tables .stat-card-title'),
                change: document.querySelector('.stat-card.tables .stat-card-change')
            }
        },
        chartContainer: document.querySelector('.chart-container')
    };

    // Initialize
    async function initialize() {
        try {
            await Promise.all([
                loadDashboardStats(),
                initRevenueChart(),
                loadTopSellingFoods(state.timeRanges.topFoods)
            ]);
            setupEventListeners();
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi tải dữ liệu", "error");
        }
    }

    // Skeleton loaders
    function showTopFoodsSkeletons() {
        DOM.topFoodsList.innerHTML = Array(5).fill().map(() => `
            <div class="food-item skeleton-item">
                <div class="food-rank skeleton"></div>
                <div class="food-image skeleton"></div>
                <div class="food-content">
                    <div class="food-title skeleton"></div>
                    <div class="food-stats">
                        <div class="food-price skeleton"></div>
                        <div class="food-sold skeleton"></div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    // Data loading
    async function loadDashboardStats() {
        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.dashboardStats);
            state.dashboardStats = result;
            renderDashboardStats(result);
        } catch (error) {
            utils.showToast("Không thể tải dữ liệu thống kê", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }
    function updateRevenueChart(data) {
        window.revenueChart.data.labels = data.labels;
        window.revenueChart.data.datasets[0].data = data.data;
        window.revenueChart.update();
        
        // Tính tổng doanh thu
        const totalRevenue = data.data.reduce((sum, value) => sum + value, 0);
        
        // Hiển thị tổng doanh thu
        const totalRevenueElement = document.getElementById('totalRevenue');
        if (totalRevenueElement) {
            totalRevenueElement.textContent = utils.formatMoney(totalRevenue);
        }
    }
    async function loadRevenueData(timeRange) {
        try {
            utils.showLoadingOverlay(true);
            state.timeRanges.revenue = timeRange;
            const result = await utils.fetchData(API.revenueData(timeRange));
            state.revenueData = result;
            updateRevenueChart(result);
        } catch (error) {
            utils.showToast("Không thể tải dữ liệu doanh thu", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function loadTopSellingFoods(timeRange) {
        try {
            utils.showLoadingOverlay(true);
            showTopFoodsSkeletons();
            state.timeRanges.topFoods = timeRange;
            const result = await utils.fetchData(API.topSellingFoods(timeRange));
            state.topSellingFoods = result;
            renderTopSellingFoods(result);
        } catch (error) {
            utils.showToast("Không thể tải dữ liệu món ăn bán chạy", "error");
            DOM.topFoodsList.innerHTML = '<div class="text-center py-4">Không thể tải dữ liệu. Vui lòng thử lại sau.</div>';
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Rendering
    function renderDashboardStats(data) {
        // Cập nhật doanh thu tháng
        DOM.statCards.revenue.title.textContent = utils.formatMoney(data.monthlyRevenue);
        
        // Cập nhật phần trăm thay đổi doanh thu
        DOM.statCards.revenue.change.innerHTML = `<i class="fas fa-arrow-${data.revenueChangePercent >= 0 ? 'up' : 'down'}"></i> ${Math.abs(data.revenueChangePercent)}% so với tháng trước`;
        DOM.statCards.revenue.change.className = `stat-card-change ${data.revenueChangePercent >= 0 ? 'positive' : 'negative'}`;
        
        // Cập nhật đơn hàng tháng
        DOM.statCards.orders.title.textContent = data.monthlyOrders.toLocaleString('vi-VN');
        
        // Cập nhật phần trăm thay đổi đơn hàng
        DOM.statCards.orders.change.innerHTML = `<i class="fas fa-arrow-${data.ordersChangePercent >= 0 ? 'up' : 'down'}"></i> ${Math.abs(data.ordersChangePercent)}% so với tháng trước`;
        DOM.statCards.orders.change.className = `stat-card-change ${data.ordersChangePercent >= 0 ? 'positive' : 'negative'}`;
        
        // Cập nhật khách hàng mới
        DOM.statCards.customers.title.textContent = data.newCustomers.toLocaleString('vi-VN');
        
        // Cập nhật phần trăm thay đổi khách hàng mới
        DOM.statCards.customers.change.innerHTML = `<i class="fas fa-arrow-${data.newCustomersChangePercent >= 0 ? 'up' : 'down'}"></i> ${Math.abs(data.newCustomersChangePercent)}% so với tháng trước`;
        DOM.statCards.customers.change.className = `stat-card-change ${data.newCustomersChangePercent >= 0 ? 'positive' : 'negative'}`;
        
        // Cập nhật tỷ lệ sử dụng bàn
        DOM.statCards.tables.title.textContent = data.tableUsagePercent + '%';
        
        // Cập nhật phần trăm thay đổi tỷ lệ sử dụng bàn
        DOM.statCards.tables.change.innerHTML = `<i class="fas fa-arrow-${data.tableUsageChangePercent >= 0 ? 'up' : 'down'}"></i> ${Math.abs(data.tableUsageChangePercent)}% so với tháng trước`;
        DOM.statCards.tables.change.className = `stat-card-change ${data.tableUsageChangePercent >= 0 ? 'positive' : 'negative'}`;
    }

    function renderTopSellingFoods(foods) {
        let foodsHTML = '';
        
        foods.forEach(food => {
            foodsHTML += `
            <div class="food-item">
                <div class="food-rank">${food.rank}</div>
                <div class="food-image">
                    <img src="/images/MenuItem/${food.image}" alt="${food.name}">
                </div>
                <div class="food-content">
                    <h6 class="food-title">${food.name}</h6>
                    <div class="food-stats">
                        <span class="food-price">${utils.formatMoney(food.price)}</span>
                        <span class="food-sold">Đã bán: ${food.soldQuantity}</span>
                    </div>
                </div>
            </div>
            `;
        });
        
        DOM.topFoodsList.innerHTML = foodsHTML || '<div class="text-center py-4">Không có dữ liệu món ăn bán chạy.</div>';
    }

    // Event listeners
    function setupEventListeners() {
        DOM.revenueTimeRange?.addEventListener("change", function() {
            loadRevenueData(this.value);
        });
        
        DOM.topFoodTimeRange?.addEventListener("change", function() {
            loadTopSellingFoods(this.value);
        });

    }

    // Chart functions
    function initRevenueChart() {
        const ctx = DOM.revenueChart.getContext('2d');
        
        // Cấu hình biểu đồ
        const config = {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Doanh thu',
                    data: [],
                    backgroundColor: 'rgba(40, 167, 69, 0.2)',
                    borderColor: '#28a745',
                    borderWidth: 2,
                    pointBackgroundColor: '#28a745',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 4,
                    pointHoverRadius: 6,
                    tension: 0.3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.7)',
                        padding: 10,
                        titleFont: {
                            size: 14,
                            weight: 'bold'
                        },
                        bodyFont: {
                            size: 13
                        },
                        callbacks: {
                            label: function(context) {
                                return utils.formatMoney(context.parsed.y);
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return utils.formatMoney(value);
                            }
                        }
                    }
                }
            }
        };
        
        // Tạo biểu đồ
        window.revenueChart = new Chart(ctx, config);
        
        // Tải dữ liệu ban đầu
        return loadRevenueData(state.timeRanges.revenue);
    }

    function updateRevenueChart(data) {
        window.revenueChart.data.labels = data.labels;
        window.revenueChart.data.datasets[0].data = data.data;
        window.revenueChart.update();
        
        // Tính tổng doanh thu
        const totalRevenue = data.data.reduce((sum, value) => sum + value, 0);
        
        // Hiển thị tổng doanh thu
        const totalRevenueElement = document.getElementById('totalRevenue');
        if (totalRevenueElement) {
            totalRevenueElement.textContent = utils.formatMoney(totalRevenue);
        }
        
        // Cập nhật tiêu đề biểu đồ theo khoảng thời gian
        const timeRangeText = getTimeRangeText(state.timeRanges.revenue);
        const chartTitle = document.querySelector('.revenue-chart-card .card-title');
        if (chartTitle) {
            chartTitle.textContent = `Doanh thu ${timeRangeText}`;
        }
    }
    
    // Hàm hỗ trợ để lấy text mô tả khoảng thời gian
    function getTimeRangeText(timeRange) {
        switch(timeRange) {
            case 'day': return 'hôm nay';
            case 'week': return 'tuần này';
            case 'month': return 'tháng này';
            case 'quarter': return 'quý này';
            case 'year': return 'năm nay';
            default: return 'tháng này';
        }
    }


    // Khởi chạy ứng dụng
    initialize();
});