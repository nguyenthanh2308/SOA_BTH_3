(() => {
    // ===== helpers (không dùng $ toàn cục) =====
    const byId = (id) => document.getElementById(id);
    const LS = { urls: "CFG_URLS", token: "TOKEN" };
    const defaults = {
        auth: "https://localhost:7268",
        product: "https://localhost:7181",
        order: "https://localhost:7182",
        report: "https://localhost:5003"
    };
    const cfg = (() => {
        try {
            const fromLs = JSON.parse(localStorage.getItem(LS.urls) || "{}");
            return { ...defaults, ...fromLs };
        } catch { return defaults; }
    })();
    const TOKEN = localStorage.getItem(LS.token) || "";

    function say(id, text, type = "info") {
        const el = byId(id);
        if (!el) return;
        el.className = "msg " + type;
        el.textContent = text || "";
    }
    function httpJson(url, opts = {}) {
        return fetch(url, opts).then(async (r) => {
            if (!r.ok) {
                const t = await r.text().catch(() => r.statusText);
                throw new Error(`${r.status} ${r.statusText} - ${t}`);
            }
            const ct = r.headers.get("content-type") || "";
            return ct.includes("application/json") ? r.json() : r.text();
        });
    }
    function authHeaders(extra = {}) {
        return TOKEN ? { ...extra, Authorization: "Bearer " + TOKEN } : extra;
    }

    // ===== guard =====
    function guard() {
        if (!TOKEN) {
            window.location.href = "index.html";
            return false;
        }
        const peek = TOKEN.slice(0, 12) + "..." + TOKEN.slice(-8);
        const peekBox = byId("tokenPeek");
        if (peekBox) peekBox.textContent = "JWT: " + peek;
        if (!cfg?.product || !cfg?.order) {
            alert("Thiếu cấu hình URL (Product/Order). Vui lòng đăng nhập lại để lưu cấu hình.");
            window.location.href = "index.html";
            return false;
        }
        return true;
    }

    // ===== Products =====
    async function loadProducts() {
        say("prodMsg", "Đang tải...");
        try {
            const items = await httpJson(`${cfg.product}/products`);
            const body = byId("tbody");
            body.innerHTML = "";
            for (const p of items) {
                const tr = document.createElement("tr");
                tr.innerHTML = `
          <td>${p.id}</td>
          <td><input value="${p.name ?? ""}"></td>
          <td><input type="number" value="${p.price ?? 0}"></td>
          <td><input type="number" value="${p.quantity ?? 0}"></td>
          <td>
            <button class="btn btn-save">Lưu</button>
            <button class="btn danger btn-del">Xóa</button>
          </td>`;
                const [ipName, ipPrice, ipQty] = tr.querySelectorAll("input");
                tr.querySelector(".btn-save").onclick = async () => {
                    try {
                        await httpJson(`${cfg.product}/products/${p.id}`, {
                            method: "PUT",
                            headers: authHeaders({ "Content-Type": "application/json" }),
                            body: JSON.stringify({
                                id: p.id,
                                name: ipName.value,
                                price: Number(ipPrice.value || 0),
                                quantity: Number(ipQty.value || 0),
                            }),
                        });
                        say("prodMsg", "Đã lưu!", "ok");
                    } catch (e) {
                        say("prodMsg", e.message, "err");
                    }
                };
                tr.querySelector(".btn-del").onclick = async () => {
                    if (!confirm("Xóa SP #" + p.id + "?")) return;
                    try {
                        await httpJson(`${cfg.product}/products/${p.id}`, {
                            method: "DELETE",
                            headers: authHeaders(),
                        });
                        await loadProducts();
                        say("prodMsg", "Đã xóa!", "ok");
                    } catch (e) {
                        say("prodMsg", e.message, "err");
                    }
                };
                body.appendChild(tr);
            }
            say("prodMsg", "");
        } catch (e) {
            say("prodMsg", e.message, "err");
        }
    }

    async function createProduct() {
        try {
            await httpJson(`${cfg.product}/products`, {
                method: "POST",
                headers: authHeaders({ "Content-Type": "application/json" }),
                body: JSON.stringify({
                    name: byId("createName").value.trim(),
                    price: Number(byId("createPrice").value || 0),
                    quantity: Number(byId("createQty").value || 0),
                }),
            });
            byId("createName").value = byId("createPrice").value = byId("createQty").value = "";
            await loadProducts();
            say("prodMsg", "Đã thêm!", "ok");
        } catch (e) {
            say("prodMsg", e.message, "err");
        }
    }

    // ===== Orders =====
    async function loadOrders() {
        say("orderMsg", "Đang tải...");
        try {
            const list = await httpJson(`${cfg.order}/orders`);
            const tb = byId("ordersBody");
            tb.innerHTML = "";
            for (const o of list) {
                const items = (o.items || []).map((i) => `${i.productName} x${i.quantity}`).join(", ");
                const tr = document.createElement("tr");
                tr.innerHTML = `
          <td>${o.id}</td>
          <td>${o.customerName || ""}</td>
          <td>${o.customerEmail || ""}</td>
          <td><span class="badge ${o.status}">${o.status}</span></td>
          <td>${o.totalAmount ?? 0}</td>
          <td>${items}</td>
          <td>
            <button class="btn small btn-ok">Complete</button>
            <button class="btn small warn btn-cancel">Cancel</button>
            <button class="btn small danger btn-del">Xóa</button>
          </td>`;
                tr.querySelector(".btn-ok").onclick = () => updateStatus(o.id, "completed");
                tr.querySelector(".btn-cancel").onclick = () => updateStatus(o.id, "cancelled");
                tr.querySelector(".btn-del").onclick = async () => {
                    if (!confirm("Xóa đơn #" + o.id + "?")) return;
                    try {
                        await httpJson(`${cfg.order}/orders/${o.id}`, { method: "DELETE", headers: authHeaders() });
                        await loadOrders();
                    } catch (e) {
                        say("orderMsg", e.message, "err");
                    }
                };
                tb.appendChild(tr);
            }
            say("orderMsg", "");
        } catch (e) {
            say("orderMsg", e.message, "err");
        }
    }

    async function updateStatus(id, status) {
        try {
            await httpJson(`${cfg.order}/orders/${id}`, {
                method: "PUT",
                headers: authHeaders({ "Content-Type": "application/json" }),
                body: JSON.stringify({ status }),
            });
            // reload both orders and products so admin sees updated stock immediately
            await loadOrders();
            await loadProducts();
            say("orderMsg", `Đã chuyển ${id} → ${status}`, "ok");
        } catch (e) {
            say("orderMsg", e.message, "err");
        }
    }

    // ===== Reports =====
    async function getReportStats(type) {
        console.log("getReportStats called with", type);
        say("reportMsg", "Đang tải thống kê...");
        const start = byId("reportStart").value;
        const end = byId("reportEnd").value;
        const q = `?startDate=${start}&endDate=${end}`;

        try {
            let url = "";
            if (type === "product") url = `${cfg.report}/api/reports/products/stats${q}`;
            else url = `${cfg.report}/api/reports/orders/summary${q}`;

            console.log("Fetching URL:", url);
            const data = await httpJson(url, { headers: authHeaders() });
            renderStats(type, data);
            say("reportMsg", "Đã tải xong!", "ok");
        } catch (e) {
            console.error(e);
            say("reportMsg", e.message, "err");
        }
    }

    function renderStats(type, data) {
        const box = byId("statsResult");
        box.innerHTML = "";

        if (type === "product") {
            // data = [ { productId, productName, currentStock, totalSold, totalRevenue, totalProfit } ]
            let html = `<table class="table"><thead><tr>
                <th>ID</th><th>Tên</th><th>Tồn</th><th>Đã bán</th><th>Doanh thu</th><th>Lợi nhuận</th>
            </tr></thead><tbody>`;
            data.forEach(p => {
                html += `<tr>
                    <td>${p.productId}</td>
                    <td>${p.productName}</td>
                    <td>${p.currentStock}</td>
                    <td>${p.totalSold}</td>
                    <td>${formatMoney(p.totalRevenue)}</td>
                    <td>${formatMoney(p.totalProfit)}</td>
                </tr>`;
            });
            html += "</tbody></table>";
            box.innerHTML = html;
        } else {
            // data = { totalOrders, totalRevenue, totalProfit, averageOrderValue, ordersByDate: [] }
            let html = `<div class="grid2">
                <div class="card"><h3>Tổng đơn: ${data.totalOrders}</h3></div>
                <div class="card"><h3>Doanh thu: ${formatMoney(data.totalRevenue)}</h3></div>
                <div class="card"><h3>Lợi nhuận: ${formatMoney(data.totalProfit)}</h3></div>
                <div class="card"><h3>TB đơn: ${formatMoney(data.averageOrderValue)}</h3></div>
            </div>`;

            html += `<h3 style="margin-top:10px">Chi tiết theo ngày</h3>
            <table class="table"><thead><tr><th>Ngày</th><th>Số đơn</th><th>Doanh thu</th></tr></thead><tbody>`;
            (data.ordersByDate || []).forEach(d => {
                html += `<tr>
                    <td>${new Date(d.date).toLocaleDateString()}</td>
                    <td>${d.count}</td>
                    <td>${formatMoney(d.revenue)}</td>
                </tr>`;
            });
            html += "</tbody></table>";
            box.innerHTML = html;
        }
    }

    async function generateReport(type) {
        console.log("generateReport called with", type);
        say("reportMsg", `Đang tạo báo cáo ${type}...`);
        const start = byId("reportStart").value;
        const end = byId("reportEnd").value;
        if (!start || !end) {
            say("reportMsg", "Vui lòng chọn ngày bắt đầu và kết thúc", "err");
            return;
        }

        try {
            const res = await httpJson(`${cfg.report}/api/reports/generate`, {
                method: "POST",
                headers: authHeaders({ "Content-Type": "application/json" }),
                body: JSON.stringify({
                    reportType: type, // "Product" or "Order"
                    startDate: start,
                    endDate: end
                })
            });
            say("reportMsg", `Tạo thành công! ID: ${res.reportId}`, "ok");
            // Auto view the report
            await getReportDetail(res.reportId);
        } catch (e) {
            say("reportMsg", e.message, "err");
        }
    }

    async function getReportDetail(id) {
        say("reportMsg", `Đang tải báo cáo #${id}...`);
        try {
            const data = await httpJson(`${cfg.report}/api/reports/${id}`, { headers: authHeaders() });
            renderReportDetail(data);
            say("reportMsg", `Đang xem báo cáo #${id}`, "ok");
        } catch (e) {
            say("reportMsg", e.message, "err");
        }
    }

    function renderReportDetail(report) {
        const box = byId("statsResult");
        box.innerHTML = "";

        let html = `<h3>Báo cáo #${report.id} (${report.reportType})</h3>
        <p>Kỳ: ${new Date(report.period).toLocaleDateString()}</p>
        <p>Ngày tạo: ${new Date(report.generatedAt).toLocaleString()}</p>
        <table class="table">
            <thead><tr><th>Key</th><th>Tên</th><th>Số lượng</th><th>Giá trị (Lợi nhuận)</th></tr></thead>
            <tbody>`;

        (report.details || []).forEach(d => {
            html += `<tr>
                <td>${d.key}</td>
                <td>${d.name}</td>
                <td>${d.quantity}</td>
                <td>${formatMoney(d.value)}</td>
            </tr>`;
        });
        html += "</tbody></table>";
        box.innerHTML = html;
    }

    function formatMoney(n) {
        return (n || 0).toLocaleString('vi-VN', { style: 'currency', currency: 'VND' });
    }

    // ===== init =====
    document.addEventListener("DOMContentLoaded", () => {
        if (!guard()) return;

        const logoutBtn = byId("btnLogout");
        if (logoutBtn) logoutBtn.onclick = () => window.logout();

        const btnCreate = byId("btnCreate");
        if (btnCreate) btnCreate.onclick = createProduct;

        const btnReload = byId("btnReload");
        if (btnReload) btnReload.onclick = loadProducts;

        const btnOrdersReload = byId("btnOrdersReload");
        if (btnOrdersReload) btnOrdersReload.onclick = loadOrders;

        // Report buttons
        if (byId("btnStatProduct")) byId("btnStatProduct").onclick = () => getReportStats("product");
        if (byId("btnStatOrder")) byId("btnStatOrder").onclick = () => getReportStats("order");
        if (byId("btnGenProductReport")) byId("btnGenProductReport").onclick = () => generateReport("Product");
        if (byId("btnGenOrderReport")) byId("btnGenOrderReport").onclick = () => generateReport("Order");

        // Set default dates (this month)
        const now = new Date();
        const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
        if (byId("reportStart")) byId("reportStart").valueAsDate = firstDay;
        if (byId("reportEnd")) byId("reportEnd").valueAsDate = now;

        loadProducts();
        loadOrders();
    });
})();
