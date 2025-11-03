// ===== Guard: tránh nạp script 2 lần =====
if (window.__APP_LOADED__) {
    console.warn("[UI] script.js was loaded twice");
}
window.__APP_LOADED__ = true;

// ===== Shortcuts / State =====
const $ = (id) => document.getElementById(id);
let TOKEN = "";

// ===== Helpers =====
function say(target, text, type = "info") {
    const box = $(target);
    if (!box) return;
    box.className = "msg " + type;
    box.textContent = text || "";
    if (!text) box.classList.remove("msg");
}
function showToken(t) {
    const box = $("tokenBox");
    if (!box) return;
    if (!t) { box.classList.add("hidden"); box.textContent = ""; return; }
    box.classList.remove("hidden");
    box.textContent = "Token: " + t;
}
async function httpJson(url, opts = {}) {
    const res = await fetch(url, opts);
    if (!res.ok) {
        const text = await res.text().catch(() => res.statusText);
        throw new Error(res.status + " " + res.statusText + " - " + text);
    }
    const ct = res.headers.get("content-type") || "";
    return ct.includes("application/json") ? res.json() : res.text();
}
// dùng function (không bị lỗi nếu file vô tình load 2 lần)
function trimSlash(s) { return String(s).replace(/\/$/, ""); }

// ===================== Auth =====================
async function login() {
    const base = trimSlash($("authBase").value);
    say("authMsg", "Đang đăng nhập...");
    try {
        const data = await httpJson(`${base}/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                userName: $("username").value.trim(),
                password: $("password").value.trim()
            })
        });
        TOKEN = data.token || "";
        showToken(TOKEN);
        say("authMsg", "Đăng nhập thành công!", "ok");
    } catch (e) {
        say("authMsg", e.message || "Login failed", "err");
    }
}

// ===================== Products =====================
async function loadProducts() {
    const base = trimSlash($("prodBase").value);
    say("listMsg", "Đang tải...");
    try {
        const data = await httpJson(`${base}/products`);
        renderTable(Array.isArray(data) ? data : []);
        say("listMsg", "");
    } catch (e) {
        say("listMsg", e.message || "Load failed", "err");
        renderTable([]);
    }
}

function renderTable(items) {
    const body = $("tbody");
    if (!body) return;
    body.innerHTML = "";
    if (!items.length) {
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 5; td.textContent = "Chưa có sản phẩm";
        tr.appendChild(td); body.appendChild(tr); return;
    }
    for (const p of items) {
        const tr = document.createElement("tr");

        const tdId = document.createElement("td");
        tdId.textContent = p.id;
        tr.appendChild(tdId);

        const tdName = document.createElement("td");
        const ipName = document.createElement("input");
        ipName.value = p.name ?? "";
        tdName.appendChild(ipName);
        tr.appendChild(tdName);

        const tdPrice = document.createElement("td");
        const ipPrice = document.createElement("input");
        ipPrice.type = "number";
        ipPrice.value = p.price ?? 0;
        tdPrice.appendChild(ipPrice);
        tr.appendChild(tdPrice);

        const tdQty = document.createElement("td");
        const ipQty = document.createElement("input");
        ipQty.type = "number";
        ipQty.value = p.quantity ?? 0;
        tdQty.appendChild(ipQty);
        tr.appendChild(tdQty);

        const tdAct = document.createElement("td");
        const btnSave = document.createElement("button");
        btnSave.textContent = "Lưu";
        btnSave.onclick = () =>
            updateProduct({
                id: p.id,
                name: ipName.value,
                price: Number(ipPrice.value),
                quantity: Number(ipQty.value),
                description: p.description ?? null
            });
        const btnDel = document.createElement("button");
        btnDel.textContent = "Xóa";
        btnDel.className = "danger";
        btnDel.onclick = () => deleteProduct(p.id);
        tdAct.appendChild(btnSave);
        tdAct.appendChild(btnDel);
        tr.appendChild(tdAct);

        body.appendChild(tr);
    }
}

async function createProduct() {
    const base = trimSlash($("prodBase").value);
    const body = {
        name: $("createName").value.trim(),
        price: Number($("createPrice").value || 0),
        quantity: Number($("createQty").value || 0)
    };
    try {
        await httpJson(`${base}/products`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                ...(TOKEN ? { Authorization: "Bearer " + TOKEN } : {})
            },
            body: JSON.stringify(body)
        });
        say("listMsg", "Đã thêm!", "ok");
        $("createName").value = "";
        $("createPrice").value = "";
        $("createQty").value = "";
        await loadProducts();
    } catch (e) {
        say("listMsg", e.message || "Create failed (check token)", "err");
    }
}

async function updateProduct(p) {
    const base = trimSlash($("prodBase").value);
    try {
        await httpJson(`${base}/products/${p.id}`, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
                ...(TOKEN ? { Authorization: "Bearer " + TOKEN } : {})
            },
            body: JSON.stringify(p)
        });
        say("listMsg", "Đã lưu!", "ok");
    } catch (e) {
        say("listMsg", e.message || "Update failed (check token / route PUT /products/{id})", "err");
    }
}

async function deleteProduct(id) {
    const base = trimSlash($("prodBase").value);
    if (!confirm("Xóa sản phẩm #" + id + "?")) return;
    try {
        await httpJson(`${base}/products/${id}`, {
            method: "DELETE",
            headers: { ...(TOKEN ? { Authorization: "Bearer " + TOKEN } : {}) }
        });
        say("listMsg", "Đã xóa!", "ok");
        await loadProducts();
    } catch (e) {
        say("listMsg", e.message || "Delete failed (check token / route DELETE /products/{id})", "err");
    }
}

// ===== Products -> dropdown dùng cho Order =====
async function populateProductSelect() {
    const base = trimSlash($("prodBase").value);
    const sel = $("ordProductSelect");
    if (!sel) return;
    sel.innerHTML = "";
    try {
        const products = await httpJson(`${base}/products`);
        if (!Array.isArray(products) || products.length === 0) {
            sel.innerHTML = `<option value="">(Chưa có sản phẩm)</option>`;
            $("ordPrice") && ($("ordPrice").value = "");
            return;
        }
        for (const p of products) {
            const opt = document.createElement("option");
            opt.value = String(p.id);
            opt.textContent = `${p.name} (còn ${p.quantity})`;
            opt.dataset.price = p.price ?? 0;
            opt.dataset.name = p.name ?? "";
            sel.appendChild(opt);
        }
        const first = sel.options[0];
        if ($("ordPrice")) $("ordPrice").value = first ? first.dataset.price : "";
    } catch (e) {
        sel.innerHTML = `<option value="">(Không tải được danh sách)</option>`;
        $("ordPrice") && ($("ordPrice").value = "");
        console.error(e);
    }
}

// ===================== Orders =====================
const orderBase = () => trimSlash($("orderBase").value);

async function loadOrders() {
    try {
        const list = await httpJson(`${orderBase()}/orders`);
        const tb = $("ordersBody");
        if (!tb) return;
        tb.innerHTML = "";
        for (const o of list) {
            const tr = document.createElement("tr");
            tr.innerHTML = `
        <td>${o.id}</td>
        <td>${o.customerName}</td>
        <td>${o.customerEmail}</td>
        <td>${o.status}</td>
        <td>${o.totalAmount}</td>
        <td>
          <button data-id="${o.id}" class="btn-complete">Complete</button>
          <button data-id="${o.id}" class="btn-cancel danger">Cancel</button>
          <button data-id="${o.id}" class="btn-del danger">Xóa</button>
        </td>`;
            tb.appendChild(tr);
        }
        document.querySelectorAll(".btn-complete").forEach(b => b.onclick = () => updateOrderStatus(b.dataset.id, "completed"));
        document.querySelectorAll(".btn-cancel").forEach(b => b.onclick = () => updateOrderStatus(b.dataset.id, "cancelled"));
        document.querySelectorAll(".btn-del").forEach(b => b.onclick = () => deleteOrder(b.dataset.id));
    } catch (e) {
        say("orderMsg", e.message || "Load orders failed", "err");
    }
}

async function createOrder() {
    const sel = $("ordProductSelect");
    if (!sel) { say("orderMsg", "Thiếu dropdown sản phẩm", "err"); return; }
    const opt = sel.options[sel.selectedIndex];
    const productId = Number(sel.value || 0);
    if (!productId) { say("orderMsg", "Vui lòng chọn sản phẩm", "err"); return; }

    const productName = opt?.dataset.name || "";
    const unitPrice = Number(opt?.dataset.price || 0);
    const quantity = Number(($("ordQty")?.value) || 0);

    const body = {
        customerName: $("ordName")?.value.trim() || "",
        customerEmail: $("ordEmail")?.value.trim() || "",
        items: [{ productId, productName, unitPrice, quantity }]
    };

    try {
        await httpJson(`${orderBase()}/orders`, {
            method: "POST",
            headers: { "Content-Type": "application/json", ...(TOKEN ? { Authorization: "Bearer " + TOKEN } : {}) },
            body: JSON.stringify(body)
        });
        say("orderMsg", "Đã tạo đơn!", "ok");
        await loadOrders();
    } catch (e) {
        say("orderMsg", e.message || "Create order failed (check token/stock/CORS)", "err");
    }
}

async function updateOrderStatus(id, status) {
    try {
        await httpJson(`${orderBase()}/orders/${id}`, {
            method: "PUT",
            headers: { "Content-Type": "application/json", ...(TOKEN ? { Authorization: "Bearer " + TOKEN } : {}) },
            body: JSON.stringify({ status })
        });
        say("orderMsg", `Đã đổi trạng thái -> ${status}`, "ok");
        await loadOrders();
    } catch (e) {
        say("orderMsg", e.message || "Update status failed", "err");
    }
}

async function deleteOrder(id) {
    if (!confirm("Xóa đơn #" + id + "?")) return;
    try {
        await httpJson(`${orderBase()}/orders/${id}`, {
            method: "DELETE",
            headers: { ...(TOKEN ? { Authorization: "Bearer " + TOKEN } : {}) }
        });
        say("orderMsg", "Đã xóa đơn!", "ok");
        await loadOrders();
    } catch (e) {
        say("orderMsg", e.message || "Delete order failed", "err");
    }
}

// ===================== DOM Ready =====================
document.addEventListener("DOMContentLoaded", () => {
    // Auth
    $("btnLogin")?.addEventListener("click", login);
    $("btnClearToken")?.addEventListener("click", () => { TOKEN = ""; showToken(""); say("authMsg", ""); });

    // Products
    $("btnReload")?.addEventListener("click", async () => {
        await loadProducts();
        await populateProductSelect();
    });
    $("btnCreate")?.addEventListener("click", createProduct);

    // Orders
    $("btnOrdersReload")?.addEventListener("click", loadOrders);
    $("btnOrderCreate")?.addEventListener("click", createOrder);

    // Cập nhật giá khi đổi sản phẩm
    const sel = $("ordProductSelect");
    sel?.addEventListener("change", () => {
        const opt = sel.options[sel.selectedIndex];
        if ($("ordPrice")) $("ordPrice").value = opt?.dataset?.price ?? "";
    });

    // Initial loads
    (async () => {
        try { await loadProducts(); } catch (e) { console.error(e); }
        try { await populateProductSelect(); } catch (e) { console.error(e); }
        try { await loadOrders(); } catch (e) { console.error(e); }
    })();
});
