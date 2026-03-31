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

(() => {
    const eye = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16" fill="currentColor" width="18" height="18"><path d="M16 8s-3-5.5-8-5.5S0 8 0 8s3 5.5 8 5.5S16 8 16 8Zm-8 4a4 4 0 1 1 0-8 4 4 0 0 1 0 8Z"/><path d="M8 5.5A2.5 2.5 0 1 0 8 10.5 2.5 2.5 0 0 0 8 5.5Z"/></svg>`;
    const eyeSlash = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16" fill="currentColor" width="18" height="18"><path d="M13.359 11.238C14.523 10.118 15.5 8.5 16 8c0 0-3-5.5-8-5.5-1.2 0-2.31.226-3.31.62l1.012 1.012A6.46 6.46 0 0 1 8 3.5c3.314 0 5.61 3.03 6.314 4-.33.454-.986 1.275-1.917 2.06l.962.978ZM2.64 4.762C1.477 5.882.5 7.5 0 8c0 0 3 5.5 8 5.5 1.2 0 2.31-.226 3.31-.62l-1.012-1.012A6.46 6.46 0 0 1 8 12.5c-3.314 0-5.61-3.03-6.314-4 .33-.454.986-1.275 1.917-2.06l-.962-.978Z"/><path d="M11.354 9.354a3 3 0 0 0-4.708-4.708l.708.708A2 2 0 0 1 10 8c0 .235-.04.46-.114.67l1.468 1.468ZM4.646 6.646a3 3 0 0 0 4.708 4.708l-.708-.708A2 2 0 0 1 6 8c0-.235.04-.46.114-.67L4.646 6.646Z"/><path d="M13.646 14.354 1.646 2.354l.708-.708 12 12-.708.708Z"/></svg>`;

    const updateButton = (button, isHidden) => {
        button.innerHTML = isHidden ? eye : eyeSlash;
        button.setAttribute("aria-label", isHidden ? "Show password" : "Hide password");
    };

    document.querySelectorAll(".js-toggle-password").forEach((button) => {
        const selector = button.getAttribute("data-target");
        if (!selector) {
            return;
        }

        const input = document.querySelector(selector);
        if (!(input instanceof HTMLInputElement)) {
            return;
        }

        updateButton(button, input.type === "password");

        button.addEventListener("click", () => {
            const isHidden = input.type === "password";
            input.type = isHidden ? "text" : "password";
            updateButton(button, !isHidden);
        });
    });
})();

(() => {
    const findTextarea = (selector) => {
        if (!selector) {
            return null;
        }
        const el = document.querySelector(selector);
        return el instanceof HTMLTextAreaElement ? el : null;
    };

    const replaceSelection = (textarea, before, after) => {
        const start = textarea.selectionStart ?? 0;
        const end = textarea.selectionEnd ?? 0;
        const value = textarea.value ?? "";
        const selected = value.slice(start, end);
        const next = value.slice(0, start) + before + selected + after + value.slice(end);
        textarea.value = next;
        const cursor = start + before.length + selected.length + after.length;
        textarea.focus();
        textarea.setSelectionRange(cursor, cursor);
        textarea.dispatchEvent(new Event("input", { bubbles: true }));
    };

    const prefixLines = (textarea, prefix) => {
        const start = textarea.selectionStart ?? 0;
        const end = textarea.selectionEnd ?? 0;
        const value = textarea.value ?? "";
        const block = value.slice(start, end);
        const lines = (block.length ? block : value.slice(start)).split(/\r?\n/);
        const prefixed = lines.map((l) => (l.startsWith(prefix) ? l : prefix + l)).join("\n");
        const next = value.slice(0, start) + prefixed + value.slice(end);
        textarea.value = next;
        textarea.focus();
        textarea.setSelectionRange(start, start + prefixed.length);
        textarea.dispatchEvent(new Event("input", { bubbles: true }));
    };

    document.addEventListener("click", (e) => {
        const button = e.target instanceof Element ? e.target.closest(".js-md-btn") : null;
        if (!button) {
            return;
        }

        const action = button.getAttribute("data-action");
        const target = button.getAttribute("data-target");
        const textarea = findTextarea(target);
        if (!textarea || !action) {
            return;
        }

        if (action === "bold") {
            replaceSelection(textarea, "**", "**");
        } else if (action === "italic") {
            replaceSelection(textarea, "_", "_");
        } else if (action === "inlineCode") {
            replaceSelection(textarea, "`", "`");
        } else if (action === "highlight") {
            replaceSelection(textarea, "==", "==");
        } else if (action === "quote") {
            prefixLines(textarea, "> ");
        } else if (action === "codeBlock") {
            replaceSelection(textarea, "```\n", "\n```");
        }
    });
})();
