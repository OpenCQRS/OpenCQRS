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

function apply() {
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
