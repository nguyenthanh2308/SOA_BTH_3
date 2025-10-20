const el = (id) => document.getElementById(id);
let TOKEN = "";

function setMsg(target, text, type = "info") {
    const box = el(target);
    if (!box) return;
    box.className = "msg " + type;
    box.textContent = text || "";
    if (!text) box.classList.remove("msg");
}

function showToken(token) {
    const box = el("tokenBox");
    if (!token) {
        box.classList.add("hidden");
        box.textContent = "";
        return;
    }
    box.classList.remove("hidden");
    box.textContent = "Token: " + token;
}

async function httpJson(url, opts = {}) {
    const res = await fetch(url, opts);
    if (!res.ok) {
        const text = await res.text().catch(() => res.statusText);
        throw new Error(`${res.status} ${res.statusText} - ${text}`);
    }
    const ct = res.headers.get("content-type") || "";
    if (ct.includes("application/json")) return await res.json();
    return await res.text();
}

// === Auth ===
async function login() {
    const base = el("authBase").value.replace(/\/$/, "");
    const username = el("username").value.trim();
    const password = el("password").value.trim();
    setMsg("authMsg", "Đang đăng nhập…");
    try {
        const data = await httpJson(`${base}/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ userName: username, password }),
        });
        TOKEN = data.token || "";
        showToken(TOKEN);
        setMsg("authMsg", "Đăng nhập thành công!", "ok");
    } catch (e) {
        setMsg("authMsg", e.message || "Login failed", "err");
    }
}

// === Products ===
async function loadProducts() {
    const base = el("prodBase").value.replace(/\/$/, "");
    setMsg("listMsg", "Đang tải…");
    try {
        const data = await httpJson(`${base}/products`);
        renderTable(data || []);
        setMsg("listMsg", "");
    } catch (e) {
        setMsg("listMsg", e.message || "Load failed", "err");
        renderTable([]);
    }
}

function renderTable(items) {
    const tbody = el("tbody");
    tbody.innerHTML = "";
    if (!Array.isArray(items) || items.length === 0) {
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 5;
        td.textContent = "Chưa có sản phẩm";
        tr.appendChild(td);
        tbody.appendChild(tr);
        return;
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
        btnSave.onclick = () => updateProduct({ id: p.id, name: ipName.value, price: Number(ipPrice.value), quantity: Number(ipQty.value) });
        const btnDel = document.createElement("button");
        btnDel.textContent = "Xóa";
        btnDel.className = "danger";
        btnDel.onclick = () => deleteProduct(p.id);
        tdAct.appendChild(btnSave);
        tdAct.appendChild(btnDel);
        tr.appendChild(tdAct);

        tbody.appendChild(tr);
    }
}

async function createProduct() {
    const base = el("prodBase").value.replace(/\/$/, "");
    const name = el("createName").value.trim();
    const price = Number(el("createPrice").value || 0);
    const quantity = Number(el("createQty").value || 0);

    try {
        await httpJson(`${base}/products`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                ...(TOKEN ? { Authorization: "Bearer " + TOKEN } : {}),
            },
            body: JSON.stringify({ name, price, quantity }),
        });
        setMsg("listMsg", "Đã thêm!", "ok");
        await loadProducts();
        el("createName").value = "";
        el("createPrice").value = "";
        el("createQty").value = "";
    } catch (e) {
        setMsg("listMsg", e.message || "Create failed (check token)", "err");
    }
}

async function updateProduct(p) {
    const base = el("prodBase").value.replace(/\/$/, "");
    try {
        await httpJson(`${base}/products/${p.id}`, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
                ...(TOKEN ? { Authorization: "Bearer " + TOKEN } : {}),
            },
            body: JSON.stringify(p),
        });
        setMsg("listMsg", "Đã lưu!", "ok");
    } catch (e) {
        setMsg("listMsg", e.message || "Update failed (check token)", "err");
    }
}

async function deleteProduct(id) {
    const base = el("prodBase").value.replace(/\/$/, "");
    if (!confirm("Xóa sản phẩm #" + id + "?")) return;
    try {
        await httpJson(`${base}/products/${id}`, {
            method: "DELETE",
            headers: {
                ...(TOKEN ? { Authorization: "Bearer " + TOKEN } : {}),
            },
        });
        setMsg("listMsg", "Đã xóa!", "ok");
        await loadProducts();
    } catch (e) {
        setMsg("listMsg", e.message || "Delete failed (check token)", "err");
    }
}

// Events
window.addEventListener("DOMContentLoaded", () => {
    el("btnLogin").addEventListener("click", login);
    el("btnClearToken").addEventListener("click", () => { TOKEN = ""; showToken(""); setMsg("authMsg", ""); });
    el("btnReload").addEventListener("click", loadProducts);
    el("btnCreate").addEventListener("click", createProduct);
    loadProducts();
});
