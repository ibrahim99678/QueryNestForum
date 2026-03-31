// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(() => {
    const badge = document.getElementById("notifCountBadge");
    const dropdownBody = document.getElementById("notifDropdownBody");

    if (!badge || !dropdownBody) {
        return;
    }

    const menu = dropdownBody.parentElement;
    if (!menu) {
        return;
    }

    const setBadge = (count) => {
        const n = Number(count) || 0;
        if (n <= 0) {
            badge.classList.add("d-none");
            badge.textContent = "";
            return;
        }

        badge.classList.remove("d-none");
        badge.textContent = n > 99 ? "99+" : String(n);
    };

    const refresh = async () => {
        try {
            const countResponse = await fetch("/Notifications/UnreadCount", { credentials: "same-origin" });
            if (countResponse.ok) {
                const data = await countResponse.json();
                setBadge(data.count);
            }

            const listResponse = await fetch("/Notifications/LatestPartial", { credentials: "same-origin" });
            if (listResponse.ok) {
                menu.innerHTML = await listResponse.text();
            }
        } catch {
        }
    };

    refresh();
    setInterval(refresh, 15000);
})();
