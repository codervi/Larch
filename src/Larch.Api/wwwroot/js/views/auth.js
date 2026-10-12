import { post } from "../api.js";
import { h, field } from "../dom.js";

// One page for both signing in and creating an account.
export function render(view, { query, onSignedIn }) {
  const registering = query.get("mode") === "register";
  const expired = query.get("expired");

  const name = h("input", { type: "text", required: true, minlength: 2, maxlength: 60, autocomplete: "name" });
  const email = h("input", { type: "email", required: true, autocomplete: "username" });
  const password = h("input", {
    type: "password", required: true, minlength: registering ? 8 : null,
    autocomplete: registering ? "new-password" : "current-password",
  });
  const error = h("p", { class: "form-error", role: "alert" }, expired ? "Your session ended. Please sign in again." : "");

  const form = h("form", {
    class: "form-narrow",
    onsubmit: async (e) => {
      e.preventDefault();
      const button = form.querySelector("button[type=submit]");
      button.disabled = true;
      error.textContent = "";
      try {
        const me = registering
          ? await post("/auth/register", { displayName: name.value.trim(), email: email.value.trim(), password: password.value })
          : await post("/auth/login", { email: email.value.trim(), password: password.value });
        onSignedIn(me);
        location.hash = "#/boards";
      } catch (err) {
        error.textContent = err.message;
        password.value = "";
        button.disabled = false;
      }
    },
  },
    registering ? field("Your name", name) : null,
    field("Email", email),
    field("Password", password),
    error,
    h("button", { class: "btn", type: "submit" }, registering ? "Create account" : "Sign in"));

  view.replaceChildren(
    h("h1", {}, registering ? "Create an account" : "Sign in"),
    form,
    h("p", {}, registering
      ? h("span", {}, "Already have one? ", h("a", { href: "#/login" }, "Sign in"))
      : h("span", {}, "New here? ", h("a", { href: "#/login?mode=register" }, "Create an account"))));
}
