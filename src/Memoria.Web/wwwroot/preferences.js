// The rows-per-page choice, remembered in this browser.
//
// How big a table's pages are is said in the address: every link, every sort arrow and every filter
// on those pages carries the size along with it, so the server needs no memory of the reader and a
// link means the same thing to whoever opens it. What is missing is only what to do when the
// address says nothing — the answer there is always ten, and a reader who works at fifty asks for
// fifty again on every table they open.
//
// So this writes the pick down when it is made, and puts it back into the address on arrival at a
// table that was not asked for at a size. Nothing else moves: the page is still rendered from the
// address, and the server still decides whether a size is one it offers.

// One key for the whole application. Both places the size can be picked — the picker under a table
// and the one on the settings page — are the same preference under two labels, not two settings
// that could disagree.
const key = "memoria.rows-per-page";

// Whether the note about approximate ordering has been waved away for good. Its own key rather than
// a field beside the size, so one preference cannot be lost by writing the other.
const orderingKey = "memoria.hide-ordering-notice";

// Storage can be missing or refused outright: a private window, a browser set to block site data,
// an embedded view. A reader who cannot be remembered still gets a working table at the size the
// address asks for, so both ways in swallow the refusal rather than letting it reach the page.
function stored() {
    try {
        return window.localStorage.getItem(key);
    } catch {
        return null;
    }
}

function remember(size) {
    try {
        window.localStorage.setItem(key, size);
    } catch {
    }
}

// The ordering note is hidden only when it was asked to be. Storage that cannot be read answers
// "no", so a reader who cannot be remembered sees the note rather than silently losing it.
function noticeHidden() {
    try {
        return window.localStorage.getItem(orderingKey) === "true";
    } catch {
        return false;
    }
}

function rememberNotice(hidden) {
    try {
        if (hidden) {
            window.localStorage.setItem(orderingKey, "true");
        } else {
            window.localStorage.removeItem(orderingKey);
        }
    } catch {
    }
}

function hideNotices() {
    for (const notice of document.querySelectorAll('[data-notice="ordering"]')) {
        notice.hidden = true;
    }
}

// Closing and hiding are different answers to the same note. Close takes away the one on this page
// and nothing more — the note is true, and it comes back on the next page that has to say it. Hide
// is the standing answer, and is written down.
document.addEventListener("click", event => {
    const control = event.target;

    if (!(control instanceof HTMLElement)) {
        return;
    }

    if (control.closest("[data-notice-close]")) {
        control.closest('[data-notice="ordering"]').hidden = true;
        return;
    }

    if (control.closest("[data-notice-hide]")) {
        rememberNotice(true);
        hideNotices();
    }
});

// The pick, wherever it was made. The picker under a table submits its form and the size lands in
// the address; the one on the settings page has no form to submit and this is all that happens. One
// listener on the document rather than one per select: the selects come and go as pages are swapped
// in, and the document does not.
document.addEventListener("change", event => {
    const select = event.target;

    if (!(select instanceof HTMLSelectElement)) {
        return;
    }

    if (select.dataset.preference === "rows-per-page" || select.closest("form.page-size")) {
        remember(select.value);
    }
});

// The settings page's own switch for the ordering note, which is the way back once the note has
// been hidden: the note is what offers to hide it, so hiding it takes the offer away with it.
document.addEventListener("change", event => {
    const box = event.target;

    if (box instanceof HTMLInputElement && box.dataset.preference === "hide-ordering-notice") {
        rememberNotice(box.checked);

        if (box.checked) {
            hideNotices();
        }
    }
});

function apply() {
    // Both settings are put back on every page, and this one first: the note is rendered for
    // everyone, so a reader who hid it should not watch it go. Before the early return below,
    // because a reader who never picked a size may still have hidden the note.
    const hidden = noticeHidden();

    if (hidden) {
        hideNotices();
    }

    for (const box of document.querySelectorAll('input[data-preference="hide-ordering-notice"]')) {
        box.checked = hidden;
    }

    const size = stored();

    if (!size) {
        return;
    }

    const picker = document.querySelector('form.page-size select[name="size"]');
    const url = new URL(window.location.href);

    // Only a page that offers a size, and only when the address has not already named one: a link
    // that says how big its page is means it, whether it came from the pager, a bookmark or someone
    // else. The stored size is checked against what this picker offers before it is used — the
    // server would fall back to the default for anything else, and this way that is one round trip
    // that does not happen.
    if (picker &&
        !url.searchParams.has("size") &&
        picker.value !== size &&
        [...picker.options].some(option => option.value === size)) {
        url.searchParams.set("size", size);

        // Replaced rather than pushed: the page at the default size is not one the reader asked
        // for, and going back should leave the table rather than step through the redirect into it.
        window.location.replace(url);
        return;
    }

    // The settings picker is the preference itself rather than a size beside a table, so it is not
    // in the address at all and is filled in from what was stored. Left alone when what was stored
    // is not on offer, so the select shows a size rather than nothing.
    for (const select of document.querySelectorAll('select[data-preference="rows-per-page"]')) {
        if ([...select.options].some(option => option.value === size)) {
            select.value = size;
        }
    }
}

apply();

// Enhanced navigation swaps a page's contents in without loading a document, so this file does not
// run again — and moving from one table to another is exactly when the size has to be put back.
// Blazor says when it has swapped; that is the other moment a table appears.
if (window.Blazor) {
    Blazor.addEventListener("enhancedload", apply);
}
