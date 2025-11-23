window.app = (() => {
    const $ = (id) => document.getElementById(id);

    const get = (k, def = "") => localStorage.getItem(k) ?? def;
    const set = (k, v) => localStorage.setItem(k, v);
    const del = (k) => localStorage.removeItem(k);

    const base = {
        auth: () => get("authBase", "https://localhost:7268").replace(/\/$/, ""),
        prod: () => get("prodBase", "https://localhost:7181").replace(/\/$/, ""),
        order: () => get("orderBase", "https://localhost:7182").replace(/\/$/, ""),
        report: () => get("reportBase", "https://localhost:5003").replace(/\/$/, "")
    };

    async function httpJson(url, opts = {}) {
        const res = await fetch(url, opts);
        if (!res.ok) throw new Error(`${res.status} ${res.statusText} - ${await res.text().catch(() => res.statusText)}`);
        const ct = res.headers.get("content-type") || "";
        return ct.includes("application/json") ? res.json() : res.text();
    }

    const token = {
        get: () => get("jwt", ""),
        set: (t) => set("jwt", t || ""),
        clear: () => del("jwt")
    };

    function authHeader() {
        const t = token.get();
        return t ? { Authorization: "Bearer " + t } : {};
    }

    function goto(page) {
        // đảm bảo base URL đã lưu
        ["authBase", "prodBase", "orderBase"].forEach(k => {
            if (!get(k)) set(k, $(k)?.value || "");
        });
        location.href = page;
    }

    return { $, get, set, del, base, httpJson, token, authHeader, goto };
})();
