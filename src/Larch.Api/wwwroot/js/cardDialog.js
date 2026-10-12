import { h, field } from "./dom.js";

// Edit dialog for one card. onSave gets the new values and returns a promise;
// the dialog stays open and shows the message if it fails.
export function openCardDialog({ card, columns, onSave, onDelete }) {
  const title = h("input", { type: "text", required: true, maxlength: 120, value: card.title });
  const description = h("textarea", { rows: 5, maxlength: 2000 }, card.description ?? "");
  const priority = h("select", {}, ["Low", "Normal", "High"].map((p) => h("option", { value: p }, p)));
  priority.value = card.priority;
  const due = h("input", { type: "date", value: card.dueDate ?? "" });
  const column = h("select", {}, columns.map((c) => h("option", { value: c.id }, c.name)));
  column.value = String(card.columnId);
  const error = h("p", { class: "form-error", role: "alert" });

  const dialog = h("dialog", { class: "dialog", "aria-label": "Edit card" });

  const form = h("form", {
    method: "dialog",
    onsubmit: async (e) => {
      e.preventDefault();
      if (!title.value.trim()) { error.textContent = "The card needs a title."; return; }
      const save = form.querySelector("button[type=submit]");
      save.disabled = true;
      error.textContent = "";
      try {
        await onSave({
          title: title.value.trim(),
          description: description.value.trim() || null,
          priority: priority.value,
          dueDate: due.value || null,
          columnId: Number(column.value),
        });
        dialog.close();
      } catch (err) {
        error.textContent = err.message;
        save.disabled = false;
      }
    },
  },
    h("h2", {}, "Edit card"),
    field("Title", title),
    field("Notes", description),
    h("div", { class: "grid-3" },
      field("Priority", priority),
      field("Due date", due),
      field("Column", column)),
    error,
    h("div", { class: "actions" },
      h("button", { class: "btn", type: "submit" }, "Save"),
      h("button", { class: "btn btn-ghost", type: "button", onclick: () => dialog.close() }, "Cancel"),
      h("button", {
        class: "btn btn-danger push", type: "button",
        onclick: async () => {
          if (!window.confirm(`Delete "${card.title}"?`)) return;
          try { await onDelete(); dialog.close(); } catch (err) { error.textContent = err.message; }
        },
      }, "Delete")));

  dialog.append(form);
  dialog.addEventListener("close", () => dialog.remove());
  document.body.append(dialog);
  dialog.showModal();
  title.focus();
  return dialog;
}
