// Page shell and a tiny hash router.
import { get, post, setUnauthorizedHandler } from "./api.js";
import { h } from "./dom.js";

const routes = [
  { path: /^\/login$/,       load: () => import("./views/auth.js"),   needsLogin: false },
  { path: /^\/boards$/,      load: () => import("./views/boards.js"), needsLogin: true },
  { path: /^\/boards\/(\d+)$/, load: () => import("./views/board.js"), needsLogin: true },
];

const app = document.getElementById("app");
const nav = h("header", { class: "topbar" });
const main = h("main", { id: "view", tabindex: "-1" });
app.append(nav, main);

let me = null; // { email, displayName } when signed in

function drawNav() {
  nav.replaceChildren(
    h("div", { class: "topbar-inner" },
      h("a", { class: "brand", href: me ? "#/boards" : "#/login" }, "Larch"),
      me
        ? h("div", { class: "topbar-right" },
            h("span", { class: "who" }, me.displayName),
            h("button", {
              class: "btn btn-small btn-ghost", type: "button",
              onclick: async () => { try { await post("/auth/logout"); } catch { /* the cookie is gone either way */ } me = null; drawNav(); location.hash = "#/login"; },
            }, "Sign out"))
        : null));
}

function parseHash() {
  const raw = location.hash.slice(1) || "/boards";
  const i = raw.indexOf("?");
  return { path: i === -1 ? raw : raw.slice(0, i), query: new URLSearchParams(i === -1 ? "" : raw.slice(i + 1)) };
}

let navigation = 0;

async function route() {
  const mine = ++navigation;
  const { path, query } = parseHash();
  drawNav();

  const found = routes.find((r) => r.path.test(path));
  if (!found) { location.hash = me ? "#/boards" : "#/login"; return; }
  if (found.needsLogin && !me) { location.hash = "#/login"; return; }
  if (!found.needsLogin && me) { location.hash = "#/boards"; return; }

  const view = h("div", { class: "view" });
  try {
    const mod = await found.load();
    if (mine !== navigation) return;
    main.replaceChildren(view);
    await mod.render(view, {
      params: found.path.exec(path).slice(1),
      query,
      onSignedIn: (user) => { me = user; drawNav(); },
    });
  } catch (err) {
    if (mine !== navigation) return;
    view.replaceChildren(
      h("h1", {}, err.status === 404 ? "Not found" : "Something went wrong"),
      h("p", {}, err.status === 404 ? "That board doesn't exist, or it isn't yours." : err.message),
      h("p", {}, h("a", { href: "#/boards" }, "Back to your boards")));
    main.replaceChildren(view);
  }
  if (mine === navigation) {
    document.title = (view.querySelector("h1")?.textContent ?? "Larch") + " | Larch";
    window.scrollTo(0, 0);
  }
}

setUnauthorizedHandler(() => { me = null; location.hash = "#/login?expired=1"; });
window.addEventListener("hashchange", route);

// find out if the cookie from last time is still good, then show the first page
try { me = await get("/auth/me"); } catch { me = null; }
route();
