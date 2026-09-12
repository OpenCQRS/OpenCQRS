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

// Which theme was chosen, if one was. Absent means nothing was chosen and the operating system is
// answering, which is a third state rather than a synonym for light — so it is stored as absent
// rather than written down as a guess. The value is read a second time by the small script in the
// head, which is what puts the theme on before the page paints; this key is the contract between
// the two, and renaming it here means renaming it there.
const themeKey = "memoria.theme";

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

function storedTheme() {
    try {
        return window.localStorage.getItem(themeKey);
    } catch {
        return null;
    }
}

// Following the operating system is the absence of a choice rather than a choice of its own, so
// picking it takes the value away instead of writing "system" down. The head script and the stamp
// below both read this key as "was anything chosen at all", and a third word stored here would
// have had to be understood in two more places to mean nothing.
function rememberTheme(theme) {
    try {
        if (theme === "dark" || theme === "light") {
            window.localStorage.setItem(themeKey, theme);
        } else {
            window.localStorage.removeItem(themeKey);
        }
    } catch {
    }
}

// Puts the chosen theme back on the root if it has gone missing, and takes it off when nothing is
// chosen any more. Enhanced navigation is why it goes missing: Blazor swaps a page in by diffing
// the new document against this one, and the document the server sends has no data-theme on it,
// because the choice is the browser's and the server has never heard of it. So the attribute is
// stripped on the way through, and the page arrives in whatever theme the operating system asks
// for. Nothing else writes the attribute, which is what makes removing it safe: with nothing
// stored there is nothing it could be saying.
function stampTheme() {
    const choice = storedTheme();
    const root = document.documentElement;

    if (choice !== "dark" && choice !== "light") {
        if (root.hasAttribute("data-theme")) {
            root.removeAttribute("data-theme");
        }

        return;
    }

    if (root.getAttribute("data-theme") !== choice) {
        root.setAttribute("data-theme", choice);
    }
}

// The row is rendered hidden and shown here, so the select never appears where nothing can drive
// it — and the page under it still follows the operating system, which is what the select would
// have said anyway.
function applyTheme() {
    stampTheme();

    const choice = storedTheme();
    const value = choice === "dark" || choice === "light" ? choice : "system";

    for (const select of document.querySelectorAll('select[data-preference="theme"]')) {
        select.value = value;
    }

    for (const row of document.querySelectorAll('[data-preference-row="theme"]')) {
        row.hidden = false;
    }
}

// Watching the attribute rather than only putting it back on enhancedload, because the strip
// happens after that event rather than before it — re-stamping there would be undone a moment
// later. An observer catches the removal whenever it comes, and its callback runs before the
// browser paints, so the theme does not flicker on the way between pages. The guard inside
// stampTheme is what stops this from answering its own write.
new MutationObserver(stampTheme).observe(document.documentElement, {
    attributes: true,
    attributeFilter: ["data-theme"]
});

// The theme changes as the select does, with nothing to submit: the choice is this browser's and
// the server is never told. Written down and put on the root in the same breath, so the page
// answers the pick rather than the next navigation.
document.addEventListener("change", event => {
    const select = event.target;

    if (!(select instanceof HTMLSelectElement) || select.dataset.preference !== "theme") {
        return;
    }

    rememberTheme(select.value);
    stampTheme();
});

function apply() {
    // First, and before the early return below: the theme is the whole application's, and a reader
    // who never picked a rows-per-page size still has one.
    applyTheme();

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
