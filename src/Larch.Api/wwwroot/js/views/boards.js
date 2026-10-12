import { get, post } from "../api.js";
import { h, toast } from "../dom.js";

export async function render(view) {
  const boards = await get("/boards");

  const name = h("input", { type: "text", maxlength: 80, placeholder: "Name of a new board", "aria-label": "Board name" });
  const form = h("form", {
    class: "inline-form",
    onsubmit: async (e) => {
      e.preventDefault();
      if (!name.value.trim()) return;
      try {
        const board = await post("/boards", { name: name.value.trim() });
        location.hash = `#/boards/${board.id}`;
      } catch (err) { toast(err.message, "error"); }
    },
  }, name, h("button", { class: "btn", type: "submit" }, "Create board"));

  view.replaceChildren(
    h("h1", {}, "Your boards"),
    form,
    boards.length
      ? h("ul", { class: "board-list" }, boards.map((b) =>
          h("li", {},
            h("a", { href: `#/boards/${b.id}` }, h("strong", {}, b.name)),
            h("p", { class: "muted" }, `${b.cardCount} ${b.cardCount === 1 ? "card" : "cards"}`),
            b.overdueCount > 0 ? h("span", { class: "chip chip-due-overdue" }, `${b.overdueCount} overdue`) : null)))
      : h("div", { class: "empty" }, h("p", {}, h("strong", {}, "No boards yet.")), h("p", {}, "Make one above. It starts with To do, Doing and Done.")));
}
