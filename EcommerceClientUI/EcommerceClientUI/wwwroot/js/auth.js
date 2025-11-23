// ===== helpers =====
const el = (id) => document.getElementById(id);
const LS = {
    urls: "CFG_URLS",
    token: "TOKEN",           // JWT admin
    customer: "CUSTOMER"      // { id, fullName, email }
};

function getCfg() {
    try { return JSON.parse(localStorage.getItem(LS.urls) || "{}"); }
    catch { return {}; }
}
function saveCfg() {
    const cfg = {
        auth: el("authBase")?.value || "https://localhost:7268",
        product: el("prodBase")?.value || "https://localhost:7181",
        order: el("orderBase")?.value || "https://localhost:7182",
        report: el("reportBase")?.value || "https://localhost:5003",
    };
    localStorage.setItem(LS.urls, JSON.stringify(cfg));
    return cfg;
}
function say(id, text, type = "info") {
    const box = el(id); if (!box) return;
    box.className = "msg " + type;
    box.textContent = text || "";
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
function redirect(href) { window.location.href = href; }

// ===== guards + logout: gán global =====
window.requireAdmin = function () {
    const t = localStorage.getItem(LS.token);
    if (!t) redirect("index.html");
    return t;
};
window.requireCustomer = function () {
    const s = localStorage.getItem(LS.customer);
    if (!s) redirect("index.html");
    try { return JSON.parse(s); } catch { redirect("index.html"); }
};
window.logout = function () {
    localStorage.removeItem(LS.token);
    localStorage.removeItem(LS.customer);
    redirect("index.html");
};

// ===== wire index.html =====
document.addEventListener("DOMContentLoaded", () => {
    // Chỉ chạy nếu đang ở index.html (có các phần tử này)
    if (!el("btnLoginAdmin") && !el("btnCusLogin") && !el("btnCusRegister")) return;

    // nạp cấu hình cũ nếu có
    const cfg = getCfg();
    if (cfg.auth && el("authBase")) el("authBase").value = cfg.auth;
    if (cfg.product && el("prodBase")) el("prodBase").value = cfg.product;
    if (cfg.order && el("orderBase")) el("orderBase").value = cfg.order;
    if (cfg.report && el("reportBase")) el("reportBase").value = cfg.report;

    el("btnSaveCfg")?.addEventListener("click", () => {
        saveCfg(); say("cfgMsg", "Đã lưu!", "ok");
    });

    // Nếu bạn có 2 nút "tab" đăng nhập/đăng ký
    document.querySelectorAll(".tab").forEach(btn => {
        btn.addEventListener("click", () => {
            document.querySelectorAll(".tab").forEach(b => b.classList.remove("active"));
            btn.classList.add("active");
            const tab = btn.dataset.tab;
            el("pane-login")?.classList.toggle("hidden", tab !== "login");
            el("pane-register")?.classList.toggle("hidden", tab !== "register");
        });
    });

    // ==== ADMIN LOGIN ====
    el("btnLoginAdmin")?.addEventListener("click", async () => {
        const c = saveCfg();
        say("adminMsg", "Đang đăng nhập...");
        try {
            const res = await httpJson(`${c.auth}/login`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    userName: el("adminUser").value.trim(),
                    password: el("adminPass").value.trim() // MD5
                })
            });
            const token = res.token || "";
            if (!token) throw new Error("Không nhận được token");
            localStorage.setItem(LS.token, token);
            say("adminMsg", "Thành công!", "ok");
            redirect("admin.html");
        } catch (e) { say("adminMsg", e.message || "Login thất bại", "err"); }
    });

    // ==== CUSTOMER LOGIN ====
    el("btnCusLogin")?.addEventListener("click", async () => {
        const c = saveCfg();
        say("cusLoginMsg", "Đang đăng nhập...");
        try {
            const res = await httpJson(`${c.order}/customers/login`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    email: el("cusEmailLogin").value.trim(),
                    password: el("cusPassLogin").value
                })
            });
            localStorage.setItem(LS.customer, JSON.stringify(res));
            say("cusLoginMsg", "Đăng nhập thành công!", "ok");
            redirect("shop.html");
        } catch (e) { say("cusLoginMsg", e.message || "Đăng nhập thất bại", "err"); }
    });

    // ==== CUSTOMER REGISTER ====
    el("btnCusRegister")?.addEventListener("click", async () => {
        const c = saveCfg();
        say("cusRegMsg", "Đang đăng ký...");
        try {
            const body = {
                fullName: el("cusFullNameReg").value.trim(),
                email: el("cusEmailReg").value.trim(),
                password: el("cusPassReg").value
            };
            const res = await httpJson(`${c.order}/customers/register`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(body)
            });
            localStorage.setItem(LS.customer, JSON.stringify(res));
            say("cusRegMsg", "Đăng ký thành công!", "ok");
            redirect("shop.html");
        } catch (e) { say("cusRegMsg", e.message || "Đăng ký thất bại", "err"); }
    });
});
